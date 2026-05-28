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

        // Supplier comes in as free text: link to an existing one (case-insensitive) or create.
        long? supplierId = null;
        var supplierName = req.SupplierName?.Trim();
        if (!string.IsNullOrEmpty(supplierName))
        {
            var existing = await db.Suppliers
                .FirstOrDefaultAsync(s => s.Name.ToLower() == supplierName.ToLower(), ct);
            if (existing is null)
            {
                existing = new Supplier { Name = supplierName };
                db.Suppliers.Add(existing);
                await db.SaveChangesAsync(ct);
            }
            supplierId = existing.Id;
        }

        var productIds = req.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        var missing = productIds.FirstOrDefault(id => !products.ContainsKey(id));
        if (missing != 0) throw new NotFoundException($"El producto {missing} no existe.");

        var purchase = new Purchase
        {
            SupplierId = supplierId,
            Date = req.Date,
            Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim()
        };

        foreach (var line in req.Items)
        {
            if (line.Paquetes <= 0) throw new BusinessRuleException("La cantidad de paquetes debe ser mayor a cero.");
            if (line.UnidadesPorPaquete < 1) throw new BusinessRuleException("Las unidades por paquete deben ser al menos 1.");
            if (line.CostoPorPaquete < 0) throw new BusinessRuleException("El costo por paquete no puede ser negativo.");

            var product = products[line.ProductId];

            purchase.Items.Add(new PurchaseItem
            {
                ProductId = product.Id,
                PurchaseUnit = "paquete",
                Quantity = line.Paquetes,
                UnitCostSnapshot = line.CostoPorPaquete,
                ConversionFactorSnapshot = line.UnidadesPorPaquete
            });

            // Stock entering the warehouse, in sale units. Also refresh the product so the
            // catalog mirrors the latest purchase (cost + units/paquete).
            product.WarehouseStock += line.Paquetes * line.UnidadesPorPaquete;
            product.LastPurchaseCost = line.CostoPorPaquete;
            product.UnitsPerPurchaseUnit = line.UnidadesPorPaquete;
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

    /// <summary>
    /// Replaces a purchase's metadata and items. Reverses the stock from the old items, applies
    /// the stock of the new items, and refreshes each affected product's last-known cost and
    /// units-per-package from the new lines. Rejected if reversing the old items would push any
    /// product's warehouse stock below zero (because stock may already have left in the meantime).
    /// </summary>
    public async Task UpdateAsync(long id, CreatePurchaseRequest req, CancellationToken ct)
    {
        if (req.Items.Count == 0)
            throw new BusinessRuleException("La compra debe tener al menos un producto.");

        var purchase = await db.Purchases
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Compra no encontrada.");

        // Reverse the stock from the existing items (validated below: no negative stock).
        foreach (var oldItem in purchase.Items)
        {
            if (oldItem.Product is null) continue;
            if (oldItem.Product.WarehouseStock < oldItem.QuantityInSaleUnits)
                throw new BusinessRuleException(
                    $"No se puede editar: ya salió inventario del producto '{oldItem.Product.Name}' y revertir lo dejaría negativo.");
            oldItem.Product.WarehouseStock -= oldItem.QuantityInSaleUnits;
        }

        // Remove old items and matching stock movements.
        db.PurchaseItems.RemoveRange(purchase.Items);
        var oldMovements = await db.StockMovements
            .Where(m => m.SourceType == "Purchase" && m.SourceId == purchase.Id)
            .ToListAsync(ct);
        db.StockMovements.RemoveRange(oldMovements);

        // Resolve supplier (same logic as Create).
        long? supplierId = null;
        var supplierName = req.SupplierName?.Trim();
        if (!string.IsNullOrEmpty(supplierName))
        {
            var existing = await db.Suppliers
                .FirstOrDefaultAsync(s => s.Name.ToLower() == supplierName.ToLower(), ct);
            if (existing is null)
            {
                existing = new Supplier { Name = supplierName };
                db.Suppliers.Add(existing);
                await db.SaveChangesAsync(ct);
            }
            supplierId = existing.Id;
        }

        purchase.SupplierId = supplierId;
        purchase.Date = req.Date;
        purchase.Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();
        purchase.Items.Clear();

        var productIds = req.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var missing = productIds.FirstOrDefault(pid => !products.ContainsKey(pid));
        if (missing != 0) throw new NotFoundException($"El producto {missing} no existe.");

        foreach (var line in req.Items)
        {
            if (line.Paquetes <= 0) throw new BusinessRuleException("La cantidad de paquetes debe ser mayor a cero.");
            if (line.UnidadesPorPaquete < 1) throw new BusinessRuleException("Las unidades por paquete deben ser al menos 1.");
            if (line.CostoPorPaquete < 0) throw new BusinessRuleException("El costo por paquete no puede ser negativo.");

            var product = products[line.ProductId];
            purchase.Items.Add(new PurchaseItem
            {
                ProductId = product.Id,
                PurchaseUnit = "paquete",
                Quantity = line.Paquetes,
                UnitCostSnapshot = line.CostoPorPaquete,
                ConversionFactorSnapshot = line.UnidadesPorPaquete
            });

            product.WarehouseStock += line.Paquetes * line.UnidadesPorPaquete;
            product.LastPurchaseCost = line.CostoPorPaquete;
            product.UnitsPerPurchaseUnit = line.UnidadesPorPaquete;
        }

        purchase.TotalCost = purchase.Items.Sum(i => i.Subtotal);
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
                Notes = $"Compra #{purchase.Id} (editada)"
            });
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Soft-deletes a purchase and reverses its stock. Records a corrective StockMovement per
    /// item with <c>SourceType="PurchaseDelete"</c> so the ledger explains the drop.
    /// </summary>
    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var purchase = await db.Purchases
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Compra no encontrada.");

        foreach (var item in purchase.Items)
        {
            if (item.Product is null) continue;
            if (item.Product.WarehouseStock < item.QuantityInSaleUnits)
                throw new BusinessRuleException(
                    $"No se puede eliminar: ya salió inventario del producto '{item.Product.Name}' y revertir lo dejaría negativo.");
        }

        foreach (var item in purchase.Items)
        {
            item.Product!.WarehouseStock -= item.QuantityInSaleUnits;
            db.StockMovements.Add(new StockMovement
            {
                ProductId = item.ProductId,
                MovementType = MovementType.Adjustment,
                Location = StockLocation.Warehouse,
                Quantity = -item.QuantityInSaleUnits,
                SourceType = "PurchaseDelete",
                SourceId = purchase.Id,
                OccurredAt = clock.UtcNow,
                Notes = $"Reversa por eliminación de compra #{purchase.Id}"
            });
        }

        db.Purchases.Remove(purchase); // soft-delete via interceptor
        await db.SaveChangesAsync(ct);
    }
}
