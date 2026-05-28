using FluentValidation;
using Merkatto.Application.Common;
using Merkatto.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Catalog;

public record LookupItem(long Id, string Name);
public record NameRequest(string Name);

public sealed class NameRequestValidator : AbstractValidator<NameRequest>
{
    public NameRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}

/// <summary>Simple management of the product lookups: categories and brands.</summary>
public sealed class CategoryBrandService(IAppDbContext db)
{
    public async Task<IReadOnlyList<LookupItem>> GetCategoriesAsync(CancellationToken ct) =>
        await db.Categories.OrderBy(c => c.Name)
            .Select(c => new LookupItem(c.Id, c.Name)).ToListAsync(ct);

    public async Task<IReadOnlyList<LookupItem>> GetBrandsAsync(CancellationToken ct) =>
        await db.Brands.OrderBy(b => b.Name)
            .Select(b => new LookupItem(b.Id, b.Name)).ToListAsync(ct);

    public async Task<long> CreateCategoryAsync(NameRequest req, CancellationToken ct)
    {
        var name = req.Name.Trim();
        if (await db.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower(), ct))
            throw new ConflictException("Ya existe una categoría con ese nombre.");
        var category = new Category { Name = name };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category.Id;
    }

    public async Task UpdateCategoryAsync(long id, NameRequest req, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Categoría no encontrada.");
        var name = req.Name.Trim();
        if (await db.Categories.AnyAsync(c => c.Id != id && c.Name.ToLower() == name.ToLower(), ct))
            throw new ConflictException("Ya existe otra categoría con ese nombre.");
        category.Name = name;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Soft-deletes a category. Rejected if any product still uses it — the operator must
    /// move those products to another category first.
    /// </summary>
    public async Task DeleteCategoryAsync(long id, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Categoría no encontrada.");
        var inUse = await db.Products.AnyAsync(p => p.CategoryId == id, ct);
        if (inUse) throw new BusinessRuleException("La categoría tiene productos asignados. Reasígnalos antes de eliminar.");
        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
    }

    public async Task<long> CreateBrandAsync(NameRequest req, CancellationToken ct)
    {
        var name = req.Name.Trim();
        if (await db.Brands.AnyAsync(b => b.Name.ToLower() == name.ToLower(), ct))
            throw new ConflictException("Ya existe una marca con ese nombre.");
        var brand = new Brand { Name = name };
        db.Brands.Add(brand);
        await db.SaveChangesAsync(ct);
        return brand.Id;
    }
}
