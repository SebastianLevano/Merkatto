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

        var rows = products
            .Select(p => new InventoryRow(p.Id, p.Name, p.SaleUnit, p.WarehouseStock, p.CounterStock,
                p.TotalStock, p.MinStock, p.IsLowStock))
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
