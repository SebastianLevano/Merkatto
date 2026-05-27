using Merkatto.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Audit;

public sealed class TimelineService(IAppDbContext db)
{
    private static readonly HashSet<string> Included =
    [
        "Product", "Purchase", "DailyClosing", "Expense",
        "CreditCustomer", "CreditSale", "CreditPayment",
        "Supplier", "User", "Category", "Brand"
    ];

    public async Task<PagedResult<TimelineEntry>> GetAsync(PagedQuery query, CancellationToken ct)
    {
        var q = db.AuditLogs
            .Where(l => Included.Contains(l.EntityName))
            .OrderByDescending(l => l.Timestamp);

        var total = await q.CountAsync(ct);
        var logs = await q.Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);

        var userIds = logs.Where(l => l.UserId != null)
            .Select(l => l.UserId!)
            .Distinct()
            .Select(id => long.TryParse(id, out var lid) ? lid : 0L)
            .Where(id => id > 0)
            .ToList();

        var emailMap = new Dictionary<string, string>();
        if (userIds.Count > 0)
        {
            var users = await db.Users.IgnoreQueryFilters()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Email })
                .ToListAsync(ct);
            foreach (var u in users)
                emailMap[u.Id.ToString()] = u.Email;
        }

        var items = logs.Select(l => new TimelineEntry(
            l.Id, l.Action, l.EntityName, l.EntityId,
            l.UserId != null && emailMap.TryGetValue(l.UserId, out var email) ? email : "sistema",
            l.Timestamp, l.IpAddress
        )).ToList();

        return new PagedResult<TimelineEntry>(items, total, query.Page, query.PageSize);
    }
}
