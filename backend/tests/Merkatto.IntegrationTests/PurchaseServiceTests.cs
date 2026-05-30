using Merkatto.Application.Catalog;
using Merkatto.Application.Common;
using Merkatto.Application.Purchasing;
using Merkatto.Domain.Catalog;
using Merkatto.Infrastructure.Persistence;
using Merkatto.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.IntegrationTests;

public abstract class PurchaseServiceTestsBase
{
    protected abstract AppDbContext NewCtx();

    private async Task<long> SeedProductAsync()
    {
        await using var ctx = NewCtx();
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
        await using (var ctx = NewCtx())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            purchaseId = await svc.CreateAsync(new CreatePurchaseRequest(
                "Distribuidora Test",
                new DateOnly(2025, 6, 1),
                null,
                new[] { new CreatePurchaseItemRequest(productId, Paquetes: 3, UnidadesPorPaquete: 6, CostoPorPaquete: 12m) }),
                CancellationToken.None);
        }

        await using var verify = NewCtx();
        var p = await verify.Products.FindAsync(productId);
        Assert.NotNull(p);
        Assert.Equal(18m, p.WarehouseStock);
        Assert.Equal(12m, p.LastPurchaseCost);
        Assert.Equal(6, p.UnitsPerPurchaseUnit);

        var purchase = await verify.Purchases.FindAsync(purchaseId);
        Assert.NotNull(purchase);
        Assert.Equal(36m, purchase.TotalCost);

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

        await using (var ctx = NewCtx())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            await svc.CreateAsync(new CreatePurchaseRequest(
                supplierName, new DateOnly(2025, 6, 2), null,
                new[] { new CreatePurchaseItemRequest(productId, 1, 12, 10m) }),
                CancellationToken.None);
        }

        await using var verify = NewCtx();
        Assert.True(await verify.Suppliers.AnyAsync(s => s.Name == supplierName));
    }

    [Fact]
    public async Task CreateAsync_ReusesExistingSupplier_WhenSameNameUsedTwice()
    {
        var productId = await SeedProductAsync();
        var supplierName = $"Proveedor-{Guid.NewGuid():N}";

        await using (var ctx = NewCtx())
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

        await using var verify = NewCtx();
        Assert.Equal(1, await verify.Suppliers.CountAsync(s => s.Name == supplierName));
    }

    [Fact]
    public async Task DeleteAsync_ReversesStockAndCreatesCorrectiveMovement()
    {
        var productId = await SeedProductAsync();

        long purchaseId;
        await using (var ctx = NewCtx())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            purchaseId = await svc.CreateAsync(new CreatePurchaseRequest(
                null, new DateOnly(2025, 7, 1), null,
                new[] { new CreatePurchaseItemRequest(productId, 2, 6, 10m) }),
                CancellationToken.None);
        }

        await using (var ctx = NewCtx())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            await svc.DeleteAsync(purchaseId, CancellationToken.None);
        }

        await using var verify = NewCtx();
        var p = await verify.Products.FindAsync(productId);
        Assert.NotNull(p);
        Assert.Equal(0m, p.WarehouseStock);

        var corrective = await verify.StockMovements
            .Where(m => m.SourceType == "PurchaseDelete" && m.SourceId == purchaseId)
            .SingleAsync();
        Assert.Equal(-12m, corrective.Quantity);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsBusinessRule_WhenRevertingWouldMakeStockNegative()
    {
        var productId = await SeedProductAsync();

        long purchaseId;
        await using (var ctx = NewCtx())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            purchaseId = await svc.CreateAsync(new CreatePurchaseRequest(
                null, new DateOnly(2025, 7, 5), null,
                new[] { new CreatePurchaseItemRequest(productId, 1, 6, 10m) }),
                CancellationToken.None);
        }

        await using (var ctx = NewCtx())
        {
            var p = await ctx.Products.FindAsync(productId);
            Assert.NotNull(p);
            p.WarehouseStock = 0m;
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx())
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
        await using (var ctx = NewCtx())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            purchaseId = await svc.CreateAsync(new CreatePurchaseRequest(
                null, new DateOnly(2025, 8, 1), null,
                new[] { new CreatePurchaseItemRequest(productId, 2, 6, 10m) }),
                CancellationToken.None);
        }

        await using (var ctx = NewCtx())
        {
            var svc = new PurchaseService(ctx, new StubClock());
            await svc.UpdateAsync(purchaseId, new CreatePurchaseRequest(
                null, new DateOnly(2025, 8, 1), null,
                new[] { new CreatePurchaseItemRequest(productId, 1, 12, 15m) }),
                CancellationToken.None);
        }

        await using var verify = NewCtx();
        var p = await verify.Products.FindAsync(productId);
        Assert.NotNull(p);
        Assert.Equal(12m, p.WarehouseStock);
        Assert.Equal(15m, p.LastPurchaseCost);
        Assert.Equal(12, p.UnitsPerPurchaseUnit);
    }
}

[Collection("Postgres")]
public sealed class PurchaseServiceTests_Postgres(PostgresFixture f) : PurchaseServiceTestsBase
{
    protected override AppDbContext NewCtx() => f.CreateContext();
}

[Collection("Sqlite")]
public sealed class PurchaseServiceTests_Sqlite(SqliteFixture f) : PurchaseServiceTestsBase
{
    protected override AppDbContext NewCtx() => f.CreateContext();
}
