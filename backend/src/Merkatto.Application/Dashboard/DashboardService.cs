using Merkatto.Application.Alerts;
using Merkatto.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Dashboard;

public sealed class DashboardService(IAppDbContext db, IDateTimeProvider clock, AlertService alertService)
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.Date);

        var lastClosing = await db.DailyClosings
            .OrderByDescending(c => c.BusinessDate)
            .Select(c => new DashboardLastClosing(c.BusinessDate, c.GrossIncome, c.NetFlow, c.EstimatedProfit))
            .FirstOrDefaultAsync(ct);

        var todayExpenses = await db.Expenses
            .Where(e => e.Date == today)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        var lowStockCount = await db.Products
            .Where(p => p.IsActive && (p.WarehouseStock + p.CounterStock) <= p.MinStock)
            .CountAsync(ct);

        // Aggregate functions on decimal-with-converter columns don't apply the converter
        // in SQLite projections. Materialize with navigation properties and compute in C#.
        var customers = await db.CreditCustomers
            .Include(c => c.Sales)
            .Include(c => c.Payments)
            .ToListAsync(ct);
        var creditRows = customers.Select(c => new
        {
            Sales = c.Sales.Sum(s => s.TotalAmount),
            Payments = c.Payments.Sum(p => p.Amount)
        }).ToList();
        var totalCredit = creditRows.Sum(r => r.Sales - r.Payments);
        var activeCustomers = creditRows.Count(r => r.Sales - r.Payments > 0);

        var recentClosings = await db.DailyClosings
            .OrderByDescending(c => c.BusinessDate)
            .Take(7)
            .Select(c => new DashboardClosingRow(c.BusinessDate, c.GrossIncome, c.NetFlow))
            .ToListAsync(ct);

        var alerts = await alertService.GetAlertsAsync(ct);

        // Fetch all active products and sort in memory — avoids integer-division in SQL
        // when columns are stored as long (fixed-point). Volume is tiny per installation.
        var allProducts = await db.Products
            .Where(p => p.IsActive && p.SalePrice > 0 && p.UnitsPerPurchaseUnit > 0)
            .Select(p => new
            {
                p.Id,
                p.Name,
                Category = p.Category != null ? p.Category.Name : null,
                p.SalePrice,
                p.LastPurchaseCost,
                p.UnitsPerPurchaseUnit
            })
            .ToListAsync(ct);

        var top = allProducts
            .Select(p =>
            {
                var unitCost = p.UnitsPerPurchaseUnit > 0 ? p.LastPurchaseCost / p.UnitsPerPurchaseUnit : 0m;
                var margin = p.SalePrice - unitCost;
                var rate = p.SalePrice > 0 ? margin / p.SalePrice : 0m;
                return new DashboardTopProduct(p.Id, p.Name, p.Category, p.SalePrice, margin, rate);
            })
            .OrderByDescending(p => p.MarginRate)
            .Take(5)
            .ToList();

        return new DashboardSummary(
            lastClosing,
            todayExpenses,
            lowStockCount,
            totalCredit,
            activeCustomers,
            recentClosings.OrderBy(r => r.Date).ToList(),
            alerts.Count,
            top
        );
    }
}
