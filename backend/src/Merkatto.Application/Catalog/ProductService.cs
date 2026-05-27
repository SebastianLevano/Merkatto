using Merkatto.Application.Common;
using Merkatto.Domain.Catalog;
using Merkatto.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Catalog;

public sealed class ProductService(IAppDbContext db, IDateTimeProvider clock)
{
    public async Task<PagedResult<ProductListItem>> GetAsync(PagedQuery query, bool? activeOnly, CancellationToken ct)
    {
        var q = db.Products.Include(p => p.Category).Include(p => p.Brand).AsQueryable();

        if (activeOnly == true) q = q.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(p => p.Name.ToLower().Contains(term)
                             || (p.InternalCode != null && p.InternalCode.ToLower().Contains(term)));
        }

        var total = await q.CountAsync(ct);
        var products = await q
            .OrderBy(p => p.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var productIds = products.Select(p => p.Id).ToList();
        var window = clock.UtcNow.AddDays(-30);
        var consumption = await db.StockMovements
            .Where(m => productIds.Contains(m.ProductId) && m.Quantity < 0 && m.OccurredAt >= window)
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Total = -g.Sum(m => m.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Total, ct);

        var items = products.Select(p =>
        {
            decimal? daysOfStock = null;
            if (consumption.TryGetValue(p.Id, out var consumed) && consumed > 0)
                daysOfStock = Math.Round(p.TotalStock / (consumed / 30m), 1);
            return ToListItem(p, daysOfStock);
        }).ToList();

        return new PagedResult<ProductListItem>(items, total, query.Page, query.PageSize);
    }

    public async Task<ProductDetail> GetByIdAsync(long id, CancellationToken ct)
    {
        var p = await db.Products.Include(x => x.Category).Include(x => x.Brand)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Producto no encontrado.");
        return ToDetail(p);
    }

    public async Task<long> CreateAsync(CreateProductRequest req, CancellationToken ct)
    {
        await EnsureCategoryExists(req.CategoryId, ct);
        await EnsureBrandExists(req.BrandId, ct);

        var product = new Product
        {
            Name = req.Name.Trim(),
            InternalCode = Normalize(req.InternalCode),
            CategoryId = req.CategoryId,
            BrandId = req.BrandId,
            PurchaseUnit = req.PurchaseUnit.Trim(),
            LastPurchaseCost = req.LastPurchaseCost,
            UnitsPerPurchaseUnit = req.UnitsPerPurchaseUnit,
            SaleUnit = req.SaleUnit.Trim(),
            SalePrice = req.SalePrice,
            MinStock = req.MinStock,
            WarehouseStock = req.InitialWarehouseStock,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        if (req.InitialWarehouseStock > 0)
        {
            db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id,
                MovementType = MovementType.Adjustment,
                Location = StockLocation.Warehouse,
                Quantity = req.InitialWarehouseStock,
                SourceType = "ProductCreate",
                SourceId = product.Id,
                OccurredAt = clock.UtcNow,
                Notes = "Stock inicial"
            });
            await db.SaveChangesAsync(ct);
        }

        return product.Id;
    }

    public async Task UpdateAsync(long id, UpdateProductRequest req, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Producto no encontrado.");

        await EnsureCategoryExists(req.CategoryId, ct);
        await EnsureBrandExists(req.BrandId, ct);

        product.Name = req.Name.Trim();
        product.InternalCode = Normalize(req.InternalCode);
        product.CategoryId = req.CategoryId;
        product.BrandId = req.BrandId;
        product.PurchaseUnit = req.PurchaseUnit.Trim();
        product.LastPurchaseCost = req.LastPurchaseCost;
        product.UnitsPerPurchaseUnit = req.UnitsPerPurchaseUnit;
        product.SaleUnit = req.SaleUnit.Trim();
        product.SalePrice = req.SalePrice;
        product.MinStock = req.MinStock;

        await db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(long id, bool active, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Producto no encontrado.");
        product.IsActive = active;
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureCategoryExists(long categoryId, CancellationToken ct)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == categoryId, ct))
            throw new NotFoundException("La categoría indicada no existe.");
    }

    private async Task EnsureBrandExists(long? brandId, CancellationToken ct)
    {
        if (brandId is { } id && !await db.Brands.AnyAsync(b => b.Id == id, ct))
            throw new NotFoundException("La marca indicada no existe.");
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProductListItem ToListItem(Product p, decimal? daysOfStock = null) => new(
        p.Id, p.Name, p.InternalCode, p.Category.Name, p.Brand?.Name, p.SaleUnit,
        p.SalePrice, p.UnitCost, p.Margin, p.MarginRate, p.WarehouseStock, p.CounterStock,
        p.TotalStock, p.MinStock, p.IsLowStock, p.IsActive, daysOfStock);

    private static ProductDetail ToDetail(Product p) => new(
        p.Id, p.Name, p.InternalCode, p.CategoryId, p.Category.Name, p.BrandId, p.Brand?.Name,
        p.PurchaseUnit, p.LastPurchaseCost, p.UnitsPerPurchaseUnit, p.SaleUnit, p.SalePrice,
        p.WarehouseStock, p.CounterStock, p.MinStock, p.UnitCost, p.Margin, p.MarginRate, p.IsActive);
}
