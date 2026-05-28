using Merkatto.Application.Catalog;
using Merkatto.Application.Common;
using Merkatto.Application.Purchasing;
using Merkatto.Domain.Catalog;
using Merkatto.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.IntegrationTests;

[Collection("Database")]
public class PurchaseServiceTests(DatabaseFixture db)
{
    private async Task<long> SeedProductAsync()
    {
        await using var ctx = db.CreateContext();
        var cat = new Category { Name = $"Cat-{Guid.NewGuid():N}" };
        ctx.Categories.Add(cat);
        await ctx.SaveChangesAsync();

        var svc = new ProductService(ctx, new StubClock());
        return await svc.CreateAsync(new CreateProductRequest(
            $"Prod-{Guid.NewGuid():N}", null, cat.Id, null, "unidad", 2m, 0,
            null, null, null), CancellationToken.None);
    }

    [Fact]
    public async Task CreateAsync_UpdatesProductStockCostAndCreatesStockMovement()
    {
        var productId = await SeedProductAsync();

        long purchaseId;
        await using (var ctx = db.CreateContext())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            purchaseId = await svc.CreateAsync(new CreatePurchaseRequest(
                "Distribuidora Test",
                new DateOnly(2025, 6, 1),
                null,
                new[] { new CreatePurchaseItemRequest(productId, Paquetes: 3, UnidadesPorPaquete: 6, CostoPorPaquete: 12m) }),
                CancellationToken.None);
        }

        await using var verify = db.CreateContext();
        var p = await verify.Products.FindAsync(productId);
        Assert.NotNull(p);
        Assert.Equal(18m, p.WarehouseStock);     // 3 paquetes × 6 unidades
        Assert.Equal(12m, p.LastPurchaseCost);
        Assert.Equal(6, p.UnitsPerPurchaseUnit);

        var purchase = await verify.Purchases.FindAsync(purchaseId);
        Assert.NotNull(purchase);
        Assert.Equal(36m, purchase.TotalCost);   // 3 × 12

        var movements = await verify.StockMovements
            .Where(m => m.SourceType == "Purchase" && m.SourceId == purchaseId)
            .ToListAsync();
        Assert.Single(movements);
        Assert.Equal(18m, movements[0].Quantity);
    }

    [Fact]
    public async Task CreateAsync_CreatesSupplier_WhenNameNotPreviouslySeen()
    {
        var productId = await SeedProductAsync();
        var supplierName = $"Proveedor-{Guid.NewGuid():N}";

        await using (var ctx = db.CreateContext())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            await svc.CreateAsync(new CreatePurchaseRequest(
                supplierName, new DateOnly(2025, 6, 2), null,
                new[] { new CreatePurchaseItemRequest(productId, 1, 12, 10m) }),
                CancellationToken.None);
        }

        await using var verify = db.CreateContext();
        Assert.True(await verify.Suppliers.AnyAsync(s => s.Name == supplierName));
    }

    [Fact]
    public async Task CreateAsync_ReusesExistingSupplier_WhenSameNameUsedTwice()
    {
        var productId = await SeedProductAsync();
        var supplierName = $"Proveedor-{Guid.NewGuid():N}";

        await using (var ctx = db.CreateContext())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            await svc.CreateAsync(new CreatePurchaseRequest(
                supplierName, new DateOnly(2025, 6, 3), null,
                new[] { new CreatePurchaseItemRequest(productId, 1, 6, 6m) }),
                CancellationToken.None);
            await svc.CreateAsync(new CreatePurchaseRequest(
                supplierName, new DateOnly(2025, 6, 4), null,
                new[] { new CreatePurchaseItemRequest(productId, 1, 6, 6m) }),
                CancellationToken.None);
        }

        await using var verify = db.CreateContext();
        Assert.Equal(1, await verify.Suppliers.CountAsync(s => s.Name == supplierName));
    }

    [Fact]
    public async Task DeleteAsync_ReversesStockAndCreatesCorrectiveMovement()
    {
        var productId = await SeedProductAsync();

        long purchaseId;
        await using (var ctx = db.CreateContext())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            purchaseId = await svc.CreateAsync(new CreatePurchaseRequest(
                null, new DateOnly(2025, 7, 1), null,
                new[] { new CreatePurchaseItemRequest(productId, 2, 6, 10m) }),
                CancellationToken.None);
        }

        await using (var ctx = db.CreateContext())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            await svc.DeleteAsync(purchaseId, CancellationToken.None);
        }

        await using var verify = db.CreateContext();
        var p = await verify.Products.FindAsync(productId);
        Assert.NotNull(p);
        Assert.Equal(0m, p.WarehouseStock);

        var corrective = await verify.StockMovements
            .Where(m => m.SourceType == "PurchaseDelete" && m.SourceId == purchaseId)
            .SingleAsync();
        Assert.Equal(-12m, corrective.Quantity);   // 2 × 6, reversed
    }

    [Fact]
    public async Task DeleteAsync_ThrowsBusinessRule_WhenRevertingWouldMakeStockNegative()
    {
        var productId = await SeedProductAsync();

        long purchaseId;
        await using (var ctx = db.CreateContext())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            purchaseId = await svc.CreateAsync(new CreatePurchaseRequest(
                null, new DateOnly(2025, 7, 5), null,
                new[] { new CreatePurchaseItemRequest(productId, 1, 6, 10m) }),
                CancellationToken.None);
        }

        // Simulate stock leaving (e.g. transfer out) by zeroing warehouse stock.
        await using (var ctx = db.CreateContext())
        {
            var p = await ctx.Products.FindAsync(productId);
            Assert.NotNull(p);
            p.WarehouseStock = 0m;
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = db.CreateContext())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                svc.DeleteAsync(purchaseId, CancellationToken.None));
        }
    }

    [Fact]
    public async Task UpdateAsync_ReplacesItemsAndStockCorrectly()
    {
        var productId = await SeedProductAsync();

        long purchaseId;
        await using (var ctx = db.CreateContext())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            // Initial: 2 paquetes × 6 = 12 units in warehouse
            purchaseId = await svc.CreateAsync(new CreatePurchaseRequest(
                null, new DateOnly(2025, 8, 1), null,
                new[] { new CreatePurchaseItemRequest(productId, 2, 6, 10m) }),
                CancellationToken.None);
        }

        await using (var ctx = db.CreateContext())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            // Update: replace with 1 paquete × 12 = 12 units but different cost
            await svc.UpdateAsync(purchaseId, new CreatePurchaseRequest(
                null, new DateOnly(2025, 8, 1), null,
                new[] { new CreatePurchaseItemRequest(productId, 1, 12, 15m) }),
                CancellationToken.None);
        }

        await using var verify = db.CreateContext();
        var p = await verify.Products.FindAsync(productId);
        Assert.NotNull(p);
        Assert.Equal(12m, p.WarehouseStock);     // 1 × 12 (old 12 reversed + new 12 added)
        Assert.Equal(15m, p.LastPurchaseCost);   // updated from new line
        Assert.Equal(12, p.UnitsPerPurchaseUnit);
    }
}
