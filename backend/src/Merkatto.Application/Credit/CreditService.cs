using FluentValidation;
using Merkatto.Application.Common;
using Merkatto.Domain.Credit;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Credit;

/// <summary>
/// Simple "fiados" for trusted customers: record quick credit sales and payments; the balance
/// is sales minus payments. Deliberately minimal — no accounts-receivable machinery.
/// </summary>
public sealed class CreditService(IAppDbContext db)
{
    public async Task<IReadOnlyList<CreditCustomerListItem>> GetCustomersAsync(string? search, CancellationToken ct)
    {
        var q = db.CreditCustomers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(c => c.Name.ToLower().Contains(term)
                             || (c.Phone != null && c.Phone.Contains(term)));
        }

        return await q
            .OrderBy(c => c.Name)
            .Select(c => new CreditCustomerListItem(
                c.Id, c.Name, c.Phone,
                (c.Sales.Sum(s => (decimal?)s.TotalAmount) ?? 0m) - (c.Payments.Sum(p => (decimal?)p.Amount) ?? 0m)))
            .ToListAsync(ct);
    }

    public async Task<CreditCustomerDetail> GetCustomerAsync(long id, CancellationToken ct)
    {
        var customer = await db.CreditCustomers.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Cliente no encontrado.");

        var sales = await db.CreditSales.Where(s => s.CustomerId == id)
            .Select(s => new CreditHistoryEntry("sale", s.Id, s.Date, s.TotalAmount, s.Notes))
            .ToListAsync(ct);
        var payments = await db.CreditPayments.Where(p => p.CustomerId == id)
            .Select(p => new CreditHistoryEntry("payment", p.Id, p.Date, p.Amount, p.Notes))
            .ToListAsync(ct);

        var balance = sales.Sum(s => s.Amount) - payments.Sum(p => p.Amount);
        var history = sales.Concat(payments)
            .OrderByDescending(h => h.Date).ThenByDescending(h => h.Id)
            .ToList();

        return new CreditCustomerDetail(customer.Id, customer.Name, customer.Phone, customer.Notes, balance, history);
    }

    public async Task<decimal> GetBalanceAsync(long id, CancellationToken ct)
    {
        if (!await db.CreditCustomers.AnyAsync(c => c.Id == id, ct))
            throw new NotFoundException("Cliente no encontrado.");
        var sales = await db.CreditSales.Where(s => s.CustomerId == id).SumAsync(s => (decimal?)s.TotalAmount, ct) ?? 0m;
        var payments = await db.CreditPayments.Where(p => p.CustomerId == id).SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        return sales - payments;
    }

    public async Task<long> CreateCustomerAsync(SaveCustomerRequest req, CancellationToken ct)
    {
        var customer = new CreditCustomer
        {
            Name = req.Name.Trim(),
            Phone = Normalize(req.Phone),
            Notes = Normalize(req.Notes)
        };
        db.CreditCustomers.Add(customer);
        await db.SaveChangesAsync(ct);
        return customer.Id;
    }

    public async Task<long> AddSaleAsync(CreateCreditSaleRequest req, CancellationToken ct)
    {
        await EnsureCustomerExists(req.CustomerId, ct);

        var hasItems = req.Items is { Count: > 0 };
        var total = hasItems ? req.Items!.Sum(i => i.LineTotal) : req.Amount ?? 0m;
        if (total <= 0) throw new BusinessRuleException("El monto del fiado debe ser mayor a cero.");

        var sale = new CreditSale
        {
            CustomerId = req.CustomerId,
            Date = req.Date,
            TotalAmount = total,
            Notes = Normalize(req.Notes)
        };
        if (hasItems)
        {
            foreach (var i in req.Items!)
                sale.Items.Add(new CreditSaleItem
                {
                    Description = i.Description.Trim(),
                    Quantity = i.Quantity <= 0 ? 1 : i.Quantity,
                    LineTotal = i.LineTotal
                });
        }

        db.CreditSales.Add(sale);
        await db.SaveChangesAsync(ct);
        return sale.Id;
    }

    public async Task<long> AddPaymentAsync(CreatePaymentRequest req, CancellationToken ct)
    {
        await EnsureCustomerExists(req.CustomerId, ct);
        if (req.Amount <= 0) throw new BusinessRuleException("El pago debe ser mayor a cero.");

        var payment = new CreditPayment
        {
            CustomerId = req.CustomerId,
            Date = req.Date,
            Amount = req.Amount,
            Notes = Normalize(req.Notes)
        };
        db.CreditPayments.Add(payment);
        await db.SaveChangesAsync(ct);
        return payment.Id;
    }

    private async Task EnsureCustomerExists(long id, CancellationToken ct)
    {
        if (!await db.CreditCustomers.AnyAsync(c => c.Id == id, ct))
            throw new NotFoundException("Cliente no encontrado.");
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class SaveCustomerValidator : AbstractValidator<SaveCustomerRequest>
{
    public SaveCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class CreatePaymentValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Date).NotEmpty();
    }
}
