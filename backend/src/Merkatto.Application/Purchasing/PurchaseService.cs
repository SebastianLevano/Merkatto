using Merkatto.Application.Common;
using Merkatto.Domain.Inventory;
using Merkatto.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Purchasing;

/// <summary>
/// Registers purchases. Each line is converted to sale (base) units using the product's current
/// conversion factor (snapshotted on the line), added to warehouse stock, and recorded in the
/// stock-movement ledger. The product's last purchase cost is refreshed so margins stay current.
/// </summary>
public sealed class PurchaseService(IAppDbContext db, IDateTimeProvider clock)
{
    public async Task<PagedResult<PurchaseListItem>> GetAsync(PagedQuery query, CancellationToken ct)
    {
        var q = db.Purchases.Include(p => p.Supplier).Include(p => p.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(p => p.Supplier != null && p.Supplier.Name.ToLower().Contains(term));
        }

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(p => p.Date).ThenByDescending(p => p.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        var items = rows
            .Select(p => new PurchaseListItem(p.Id, p.Date, p.Supplier?.Name, p.Items.Count, p.TotalCost))
            .ToList();
        return new PagedResult<PurchaseListItem>(items, total, query.Page, query.PageSize);
    }

    public async Task<PurchaseDetail> GetByIdAsync(long id, CancellationToken ct)
    {
        var p = await db.Purchases
            .Include(x => x.Supplier)
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Compra no encontrada.");

        var items = p.Items.Select(i => new PurchaseItemDetail(
            i.ProductId, i.Product.Name, i.PurchaseUnit, i.Quantity, i.UnitCostSnapshot,
            i.ConversionFactorSnapshot, i.QuantityInSaleUnits, i.Subtotal)).ToList();

        return new PurchaseDetail(p.Id, p.Date, p.SupplierId, p.Supplier?.Name, p.Notes, p.TotalCost, items);
    }

    public async Task<long> CreateAsync(CreatePurchaseRequest req, CancellationToken ct)
    {
        if (req.Items.Count == 0)
            throw new BusinessRuleException("La compra debe tener al menos un producto.");

        if (req.SupplierId is { } sid && !await db.Suppliers.AnyAsync(s => s.Id == sid, ct))
            throw new NotFoundException("El proveedor indicado no existe.");

        var productIds = req.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        var missing = productIds.FirstOrDefault(id => !products.ContainsKey(id));
        if (missing != 0) throw new NotFoundException($"El producto {missing} no existe.");

        var purchase = new Purchase
        {
            SupplierId = req.SupplierId,
            Date = req.Date,
            Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim()
        };

        foreach (var line in req.Items)
        {
            var product = products[line.ProductId];
            var factor = Math.Max(product.UnitsPerPurchaseUnit, 1);

            purchase.Items.Add(new PurchaseItem
            {
                ProductId = product.Id,
                PurchaseUnit = product.PurchaseUnit,
                Quantity = line.Quantity,
                UnitCostSnapshot = line.UnitCost,
                ConversionFactorSnapshot = factor
            });

            // Convert to sale units, add to warehouse, and refresh last purchase cost.
            product.WarehouseStock += line.Quantity * factor;
            product.LastPurchaseCost = line.UnitCost;
        }

        purchase.TotalCost = purchase.Items.Sum(i => i.Subtotal);
        db.Purchases.Add(purchase);
        await db.SaveChangesAsync(ct);

        foreach (var item in purchase.Items)
        {
            db.StockMovements.Add(new StockMovement
            {
                ProductId = item.ProductId,
                MovementType = MovementType.Purchase,
                Location = StockLocation.Warehouse,
                Quantity = item.QuantityInSaleUnits,
                SourceType = "Purchase",
                SourceId = purchase.Id,
                OccurredAt = clock.UtcNow,
                Notes = $"Compra #{purchase.Id}"
            });
        }
        await db.SaveChangesAsync(ct);

        return purchase.Id;
    }
}
