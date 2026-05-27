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
