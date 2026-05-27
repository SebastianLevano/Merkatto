using FluentValidation;
using Merkatto.Application.Common;
using Merkatto.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Purchasing;

public sealed class SupplierService(IAppDbContext db)
{
    public async Task<IReadOnlyList<SupplierItem>> GetAsync(CancellationToken ct) =>
        await db.Suppliers.OrderBy(s => s.Name)
            .Select(s => new SupplierItem(s.Id, s.Name, s.Phone, s.Notes))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SaveSupplierRequest req, CancellationToken ct)
    {
        var supplier = new Supplier
        {
            Name = req.Name.Trim(),
            Phone = Normalize(req.Phone),
            Notes = Normalize(req.Notes)
        };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);
        return supplier.Id;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class CreatePurchaseValidator : AbstractValidator<CreatePurchaseRequest>
{
    public CreatePurchaseValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class SaveSupplierValidator : AbstractValidator<SaveSupplierRequest>
{
    public SaveSupplierValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Phone).MaximumLength(30);
    }
}
