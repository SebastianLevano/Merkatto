using Merkatto.Application.Common;
using Merkatto.Domain.Catalog;
using Merkatto.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Inventory;

/// <summary>
/// Hybrid inventory: warehouse (sealed) and counter (open) stock per product, both in sale units.
/// Transfers move stock warehouse→counter; adjustments record losses/expired/internal-use/
/// corrections. Every change is appended to the <see cref="StockMovement"/> ledger.
/// </summary>
public sealed class InventoryService(IAppDbContext db, IDateTimeProvider clock)
{
    public async Task<PagedResult<InventoryRow>> GetAsync(PagedQuery query, bool lowStockOnly, CancellationToken ct)
    {
        var q = db.Products.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(p => p.Name.ToLower().Contains(term));
        }

        var total = await q.CountAsync(ct);
        var products = await q.OrderBy(p => p.Name).Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);

        var productIds = products.Select(p => p.Id).ToList();
        var window = clock.UtcNow.AddDays(-30);
        var consumption = await db.StockMovements
            .Where(m => productIds.Contains(m.ProductId) && m.Quantity < 0 && m.OccurredAt >= window)
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Total = -g.Sum(m => m.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Total, ct);

        var rows = products
            .Select(p =>
            {
                decimal? daysOfStock = null;
                if (consumption.TryGetValue(p.Id, out var consumed) && consumed > 0)
                    daysOfStock = Math.Round(p.TotalStock / (consumed / 30m), 1);
                return new InventoryRow(p.Id, p.Name, p.SaleUnit, p.WarehouseStock, p.CounterStock,
                    p.TotalStock, p.MinStock, p.IsLowStock, daysOfStock);
            })
            .Where(r => !lowStockOnly || r.IsLowStock)
            .ToList();

        // When filtering low-stock, total reflects the filtered set for the current page.
        return new PagedResult<InventoryRow>(rows, lowStockOnly ? rows.Count : total, query.Page, query.PageSize);
    }

    public async Task TransferAsync(TransferRequest req, CancellationToken ct)
    {
        if (req.Quantity <= 0) throw new BusinessRuleException("La cantidad a transferir debe ser mayor a cero.");

        var product = await GetProductAsync(req.ProductId, ct);
        if (product.WarehouseStock < req.Quantity)
            throw new BusinessRuleException("No hay suficiente stock en almacén para transferir.");

        product.WarehouseStock -= req.Quantity;
        product.CounterStock += req.Quantity;

        var note = string.IsNullOrWhiteSpace(req.Notes) ? "Transferencia a mostrador" : req.Notes.Trim();
        db.StockMovements.Add(NewMovement(product.Id, MovementType.Transfer, StockLocation.Warehouse, -req.Quantity, "Transfer", note));
        db.StockMovements.Add(NewMovement(product.Id, MovementType.Transfer, StockLocation.Counter, req.Quantity, "Transfer", note));

        await db.SaveChangesAsync(ct);
    }

    public async Task AdjustAsync(AdjustmentRequest req, CancellationToken ct)
    {
        if (req.Quantity == 0) throw new BusinessRuleException("El ajuste no puede ser cero.");

        var product = await GetProductAsync(req.ProductId, ct);
        var current = req.Location == StockLocation.Warehouse ? product.WarehouseStock : product.CounterStock;
        if (current + req.Quantity < 0)
            throw new BusinessRuleException("El ajuste dejaría el stock en negativo.");

        if (req.Location == StockLocation.Warehouse) product.WarehouseStock += req.Quantity;
        else product.CounterStock += req.Quantity;

        var adjustment = new InventoryAdjustment
        {
            ProductId = product.Id,
            Type = req.Type,
            Location = req.Location,
            Quantity = req.Quantity,
            Reason = string.IsNullOrWhiteSpace(req.Reason) ? null : req.Reason.Trim(),
            Date = clock.Today
        };
        db.InventoryAdjustments.Add(adjustment);
        await db.SaveChangesAsync(ct);

        db.StockMovements.Add(new StockMovement
        {
            ProductId = product.Id,
            MovementType = MovementType.Adjustment,
            Location = req.Location,
            Quantity = req.Quantity,
            SourceType = "Adjustment",
            SourceId = adjustment.Id,
            OccurredAt = clock.UtcNow,
            Notes = adjustment.Reason ?? req.Type.ToString()
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<MovementRow>> GetMovementsAsync(long? productId, PagedQuery query, CancellationToken ct)
    {
        var q = db.StockMovements.Include(m => m.Product).AsQueryable();
        if (productId is { } id) q = q.Where(m => m.ProductId == id);

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(m => m.OccurredAt).ThenByDescending(m => m.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .Select(m => new MovementRow(m.Id, m.ProductId, m.Product.Name, m.OccurredAt,
                m.MovementType, m.Location, m.Quantity, m.Notes))
            .ToListAsync(ct);

        return new PagedResult<MovementRow>(rows, total, query.Page, query.PageSize);
    }

    public async Task BatchCountAsync(BatchCountRequest req, CancellationToken ct)
    {
        if (req.Items.Count == 0) return;

        var productIds = req.Items.Select(i => i.ProductId).ToList();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync(ct);
        var map = products.ToDictionary(p => p.Id);

        // The count is "as of" a specific business date — default today. Don't allow future dates.
        var countDate = req.Date ?? clock.Today;
        if (countDate > clock.Today) countDate = clock.Today;
        // Stamp the ledger at noon UTC of the count's business date; close enough for ordering.
        var occurredAt = new DateTimeOffset(countDate.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);
        var notes = string.IsNullOrWhiteSpace(req.Notes) ? "Conteo diario" : req.Notes.Trim();
        var pending = new List<(InventoryAdjustment Adj, long ProductId, StockLocation Location, decimal Delta)>();

        foreach (var item in req.Items)
        {
            if (!map.TryGetValue(item.ProductId, out var product)) continue;
            if (item.WarehouseStock < 0 || item.CounterStock < 0) continue;

            var wd = item.WarehouseStock - product.WarehouseStock;
            var cd = item.CounterStock - product.CounterStock;

            if (wd != 0)
            {
                product.WarehouseStock = item.WarehouseStock;
                var adj = new InventoryAdjustment
                {
                    ProductId = product.Id, Type = AdjustmentType.Correction,
                    Location = StockLocation.Warehouse, Quantity = wd, Reason = notes, Date = countDate
                };
                db.InventoryAdjustments.Add(adj);
                pending.Add((adj, product.Id, StockLocation.Warehouse, wd));
            }

            if (cd != 0)
            {
                product.CounterStock = item.CounterStock;
                var adj = new InventoryAdjustment
                {
                    ProductId = product.Id, Type = AdjustmentType.Correction,
                    Location = StockLocation.Counter, Quantity = cd, Reason = notes, Date = countDate
                };
                db.InventoryAdjustments.Add(adj);
                pending.Add((adj, product.Id, StockLocation.Counter, cd));
            }
        }

        await db.SaveChangesAsync(ct);

        foreach (var (adj, productId, location, delta) in pending)
            db.StockMovements.Add(new StockMovement
            {
                ProductId = productId, MovementType = MovementType.Adjustment,
                Location = location, Quantity = delta, SourceType = "BatchCount",
                SourceId = adj.Id, OccurredAt = occurredAt, Notes = notes
            });

        if (pending.Count > 0) await db.SaveChangesAsync(ct);
    }

    private async Task<Product> GetProductAsync(long id, CancellationToken ct) =>
        await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
        ?? throw new NotFoundException("Producto no encontrado.");

    private StockMovement NewMovement(long productId, MovementType type, StockLocation location,
        decimal quantity, string sourceType, string? notes) => new()
    {
        ProductId = productId,
        MovementType = type,
        Location = location,
        Quantity = quantity,
        SourceType = sourceType,
        SourceId = productId,
        OccurredAt = clock.UtcNow,
        Notes = notes
    };
}
