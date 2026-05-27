using Merkatto.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Alerts;

public sealed class AlertService(IAppDbContext db, IDateTimeProvider clock)
{
    private static readonly decimal HighCreditThreshold = 200m;

    public async Task<IReadOnlyList<AlertItem>> GetAlertsAsync(CancellationToken ct)
    {
        var alerts = new List<AlertItem>();

        await CheckStockAsync(alerts, ct);
        await CheckClosureAsync(alerts, ct);
        await CheckCreditAsync(alerts, ct);

        return alerts;
    }

    private async Task CheckStockAsync(List<AlertItem> alerts, CancellationToken ct)
    {
        var products = await db.Products
            .Where(p => p.IsActive && (p.WarehouseStock + p.CounterStock) <= p.MinStock)
            .Select(p => new { p.Name, p.WarehouseStock, p.CounterStock })
            .ToListAsync(ct);

        if (products.Count == 0) return;

        var outOfStock = products.Where(p => p.WarehouseStock + p.CounterStock <= 0).ToList();
        var low = products.Where(p => p.WarehouseStock + p.CounterStock > 0).ToList();

        if (outOfStock.Count > 0)
            alerts.Add(new AlertItem(
                AlertType.StockOut,
                AlertSeverity.Critical,
                $"{outOfStock.Count} producto{(outOfStock.Count > 1 ? "s" : "")} sin stock",
                string.Join(", ", outOfStock.Take(5).Select(p => p.Name))
            ));

        if (low.Count > 0)
            alerts.Add(new AlertItem(
                AlertType.StockLow,
                AlertSeverity.Warning,
                $"{low.Count} producto{(low.Count > 1 ? "s" : "")} con stock bajo",
                string.Join(", ", low.Take(5).Select(p => p.Name))
            ));
    }

    private async Task CheckClosureAsync(List<AlertItem> alerts, CancellationToken ct)
    {
        var lastClosing = await db.DailyClosings
            .OrderByDescending(c => c.BusinessDate)
            .Select(c => c.BusinessDate)
            .FirstOrDefaultAsync(ct);

        if (lastClosing == default) return; // no closings yet — not an alert

        var today = DateOnly.FromDateTime(clock.UtcNow.Date);
        var daysSince = today.DayNumber - lastClosing.DayNumber;

        if (daysSince >= 2)
            alerts.Add(new AlertItem(
                AlertType.NoClosure,
                daysSince >= 4 ? AlertSeverity.Critical : AlertSeverity.Warning,
                $"Llevas {daysSince} día{(daysSince > 1 ? "s" : "")} sin registrar un cierre diario",
                $"Último cierre: {lastClosing:dd/MM/yyyy}"
            ));
    }

    private async Task CheckCreditAsync(List<AlertItem> alerts, CancellationToken ct)
    {
        var customers = await db.CreditCustomers
            .Select(c => new
            {
                c.Name,
                Balance = (c.Sales.Sum(s => (decimal?)s.TotalAmount) ?? 0m)
                        - (c.Payments.Sum(p => (decimal?)p.Amount) ?? 0m)
            })
            .Where(c => c.Balance >= HighCreditThreshold)
            .OrderByDescending(c => c.Balance)
            .ToListAsync(ct);

        if (customers.Count == 0) return;

        alerts.Add(new AlertItem(
            AlertType.HighPendingCredit,
            AlertSeverity.Warning,
            $"{customers.Count} cliente{(customers.Count > 1 ? "s" : "")} con fiado ≥ S/ {HighCreditThreshold:0}",
            string.Join(", ", customers.Take(3).Select(c => $"{c.Name} (S/{c.Balance:0.00})"))
        ));
    }
}
