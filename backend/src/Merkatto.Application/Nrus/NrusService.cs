using Merkatto.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Nrus;

public sealed class NrusService(IAppDbContext db, IDateTimeProvider clock)
{
    public async Task<NrusMonthEstimate> GetEstimateAsync(int year, int month, CancellationToken ct)
    {
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        // GrossIncome is a computed (unmapped) property — sum the persisted columns instead.
        var income = await db.DailyClosings
            .Where(c => c.BusinessDate >= from && c.BusinessDate <= to)
            .SumAsync(c => (decimal?)(c.CashAmount + c.YapeAmount + c.PlinAmount + c.PosAmount), ct) ?? 0m;

        var purchases = await db.Purchases
            .Where(p => p.Date >= from && p.Date <= to)
            .SumAsync(p => (decimal?)p.TotalCost, ct) ?? 0m;

        var max = Math.Max(income, purchases);
        var (category, quota, exceeds) = NrusBrackets.Classify(max);

        return new NrusMonthEstimate(year, month, income, purchases, max, category, quota, exceeds);
    }

    public async Task<IReadOnlyList<NrusMonthEstimate>> GetHistoryAsync(int months, CancellationToken ct)
    {
        var today = clock.UtcNow.Date;
        var results = new List<NrusMonthEstimate>(months);

        for (var i = 0; i < months; i++)
        {
            var d = today.AddMonths(-i);
            results.Add(await GetEstimateAsync(d.Year, d.Month, ct));
        }

        return results;
    }
}
