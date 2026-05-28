using FluentValidation;
using Merkatto.Application.Common;
using Merkatto.Domain.Operations;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Operations;

/// <summary>
/// End-of-day cash-up. Income is captured per payment method; the day's already-registered
/// expenses are summed automatically; profit is a referential estimate (gross income × the
/// catalog's average margin rate), since individual sales are not recorded.
/// </summary>
public sealed class DailyClosingService(IAppDbContext db, IDateTimeProvider clock)
{
    public async Task<PagedResult<DailyClosingListItem>> GetAsync(
        DateOnly? from, DateOnly? to, PagedQuery query, CancellationToken ct)
    {
        var q = db.DailyClosings.AsQueryable();
        if (from is { } f) q = q.Where(c => c.BusinessDate >= f);
        if (to is { } t) q = q.Where(c => c.BusinessDate <= t);

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(c => c.BusinessDate)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        var items = rows
            .Select(c => new DailyClosingListItem(c.Id, c.BusinessDate, c.GrossIncome, c.NetFlow, c.EstimatedProfit))
            .ToList();
        return new PagedResult<DailyClosingListItem>(items, total, query.Page, query.PageSize);
    }

    public async Task<DailyClosingDetail> GetByDateAsync(DateOnly date, CancellationToken ct)
    {
        var c = await db.DailyClosings.FirstOrDefaultAsync(x => x.BusinessDate == date, ct)
            ?? throw new NotFoundException("No hay cierre para esa fecha.");
        return ToDetail(c);
    }

    public async Task<DailyClosingDetail> GetByIdAsync(long id, CancellationToken ct)
    {
        var c = await db.DailyClosings.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Cierre no encontrado.");
        return ToDetail(c);
    }

    public async Task<DailyClosingPreview> PreviewAsync(DateOnly date, CancellationToken ct)
    {
        var expenses = await db.Expenses.Where(e => e.Date == date).SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;
        var alreadyClosed = await db.DailyClosings.AnyAsync(c => c.BusinessDate == date, ct);
        return new DailyClosingPreview(date, expenses, alreadyClosed);
    }

    public async Task<long> CreateAsync(CreateDailyClosingRequest req, CancellationToken ct)
    {
        if (await db.DailyClosings.AnyAsync(c => c.BusinessDate == req.BusinessDate, ct))
            throw new ConflictException("Ya existe un cierre para esa fecha.");

        var totalExpenses = await db.Expenses
            .Where(e => e.Date == req.BusinessDate).SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        var closing = new DailyClosing
        {
            BusinessDate = req.BusinessDate,
            CashAmount = req.CashAmount,
            YapeAmount = req.YapeAmount,
            PlinAmount = req.PlinAmount,
            PosAmount = req.PosAmount,
            PosCommissionRate = req.PosCommissionRate,
            QuickPurchases = req.QuickPurchases,
            TotalExpenses = totalExpenses,
            Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim(),
            ClosedAt = clock.UtcNow
        };
        closing.EstimatedProfit = await EstimateProfitAsync(closing.GrossIncome, ct);

        db.DailyClosings.Add(closing);
        await db.SaveChangesAsync(ct);
        return closing.Id;
    }

    /// <summary>
    /// Updates an existing closing. Only allowed within the last 7 days (relative to today's
    /// business date) to keep older reports stable. Admin-only at the controller layer.
    /// </summary>
    public async Task UpdateAsync(long id, CreateDailyClosingRequest req, CancellationToken ct)
    {
        var closing = await db.DailyClosings.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Cierre no encontrado.");

        var daysAgo = clock.Today.DayNumber - closing.BusinessDate.DayNumber;
        if (daysAgo > 7)
            throw new BusinessRuleException("Solo se pueden editar cierres de los últimos 7 días.");

        // Refresh totalExpenses in case expenses changed since the closing was created.
        var totalExpenses = await db.Expenses
            .Where(e => e.Date == closing.BusinessDate).SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        closing.CashAmount = req.CashAmount;
        closing.YapeAmount = req.YapeAmount;
        closing.PlinAmount = req.PlinAmount;
        closing.PosAmount = req.PosAmount;
        closing.PosCommissionRate = req.PosCommissionRate;
        closing.QuickPurchases = req.QuickPurchases;
        closing.TotalExpenses = totalExpenses;
        closing.Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();
        closing.EstimatedProfit = await EstimateProfitAsync(closing.GrossIncome, ct);

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Referential profit estimate: gross income × average product margin rate.</summary>
    private async Task<decimal> EstimateProfitAsync(decimal grossIncome, CancellationToken ct)
    {
        if (grossIncome <= 0) return 0m;

        var products = await db.Products
            .Where(p => p.IsActive && p.SalePrice > 0)
            .Select(p => new { p.SalePrice, p.LastPurchaseCost, p.UnitsPerPurchaseUnit })
            .ToListAsync(ct);

        if (products.Count == 0) return 0m;

        var rates = products
            .Select(p =>
            {
                var unitCost = p.UnitsPerPurchaseUnit > 0 ? p.LastPurchaseCost / p.UnitsPerPurchaseUnit : 0m;
                return p.SalePrice > 0 ? (p.SalePrice - unitCost) / p.SalePrice : 0m;
            })
            .ToList();

        var avgRate = rates.Average();
        return Math.Round(grossIncome * avgRate, 2);
    }

    private static DailyClosingDetail ToDetail(DailyClosing c) => new(
        c.Id, c.BusinessDate, c.CashAmount, c.YapeAmount, c.PlinAmount, c.PosAmount,
        c.PosCommissionRate, c.PosCommissionAmount, c.TotalExpenses, c.QuickPurchases,
        c.GrossIncome, c.NetFlow, c.EstimatedProfit, c.Notes, c.ClosedAt);
}

public sealed class CreateDailyClosingValidator : AbstractValidator<CreateDailyClosingRequest>
{
    public CreateDailyClosingValidator()
    {
        RuleFor(x => x.BusinessDate).NotEmpty();
        RuleFor(x => x.CashAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.YapeAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PlinAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PosAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PosCommissionRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.QuickPurchases).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
