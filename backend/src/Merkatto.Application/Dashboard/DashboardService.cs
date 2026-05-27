using Merkatto.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Dashboard;

public sealed class DashboardService(IAppDbContext db, IDateTimeProvider clock)
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.Date);

        var lastClosingTask = db.DailyClosings
            .OrderByDescending(c => c.BusinessDate)
            .Select(c => new DashboardLastClosing(c.BusinessDate, c.GrossIncome, c.NetFlow, c.EstimatedProfit))
            .FirstOrDefaultAsync(ct);

        var todayExpensesTask = db.Expenses
            .Where(e => e.Date == today)
            .SumAsync(e => (decimal?)e.Amount, ct);

        var lowStockTask = db.Products
            .Where(p => p.IsActive && (p.WarehouseStock + p.CounterStock) <= p.MinStock)
            .CountAsync(ct);

        var creditBalanceTask = db.CreditCustomers
            .Select(c => new
            {
                Sales = c.Sales.Sum(s => (decimal?)s.TotalAmount) ?? 0m,
                Payments = c.Payments.Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .ToListAsync(ct);

        var recentClosingsTask = db.DailyClosings
            .OrderByDescending(c => c.BusinessDate)
            .Take(7)
            .Select(c => new DashboardClosingRow(c.BusinessDate, c.GrossIncome, c.NetFlow))
            .ToListAsync(ct);

        await Task.WhenAll(lastClosingTask, todayExpensesTask, lowStockTask, creditBalanceTask, recentClosingsTask);

        var creditRows = await creditBalanceTask;
        var totalCredit = creditRows.Sum(r => r.Sales - r.Payments);
        var activeCustomers = creditRows.Count(r => r.Sales - r.Payments > 0);

        return new DashboardSummary(
            await lastClosingTask,
            await todayExpensesTask ?? 0m,
            await lowStockTask,
            totalCredit,
            activeCustomers,
            (await recentClosingsTask).OrderBy(r => r.Date).ToList()
        );
    }
}
