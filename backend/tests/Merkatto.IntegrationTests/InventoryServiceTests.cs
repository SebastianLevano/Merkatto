using Merkatto.Application.Catalog;
using Merkatto.Application.Common;
using Merkatto.Application.Inventory;
using Merkatto.Domain.Catalog;
using Merkatto.Domain.Inventory;
using Merkatto.Infrastructure.Persistence;
using Merkatto.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.IntegrationTests;

public abstract class InventoryServiceTestsBase
{
    protected abstract AppDbContext NewCtx();

    private async Task<long> SeedProductWithStockAsync(decimal warehouse, decimal counter = 0m)
    {
        await using var ctx = NewCtx();
        var cat = new Category { Name = $"Cat-{Guid.NewGuid():N}" };
        ctx.Categories.Add(cat);
        await ctx.SaveChangesAsync();

        var svc = new ProductService(ctx, new StubClock());
        var id = await svc.CreateAsync(new CreateProductRequest(
            $"Prod-{Guid.NewGuid():N}", null, cat.Id, null, "unidad", 2m, 0,
            null, null, null), CancellationToken.None);

        var p = await ctx.Products.FindAsync(id);
        Assert.NotNull(p);
        p.WarehouseStock = warehouse;
        p.CounterStock = counter;
        await ctx.SaveChangesAsync();

        return id;
    }

    [Fact]
    public async Task TransferAsync_MovesStockFromWarehouseToCounter()
    {
        var productId = await SeedProductWithStockAsync(warehouse: 20m, counter: 5m);

        await using (var ctx = NewCtx())
        {
            var svc = new InventoryService(ctx, new StubClock());
            await svc.TransferAsync(new TransferRequest(productId, 8m, null), CancellationToken.None);
        }

        await using var verify = NewCtx();
        var p = await verify.Products.FindAsync(productId);
        Assert.NotNull(p);
        Assert.Equal(12m, p.WarehouseStock);
        Assert.Equal(13m, p.CounterStock);

        var movements = await verify.StockMovements
            .Where(m => m.ProductId == productId && m.SourceType == "Transfer")
            .ToListAsync();
        Assert.Equal(2, movements.Count);
        Assert.Contains(movements, m => m.Location == StockLocation.Warehouse && m.Quantity == -8m);
        Assert.Contains(movements, m => m.Location == StockLocation.Counter && m.Quantity == 8m);
    }

    [Fact]
    public async Task TransferAsync_ThrowsBusinessRule_WhenInsufficientWarehouseStock()
    {
        var productId = await SeedProductWithStockAsync(warehouse: 3m);

        await using var ctx = NewCtx();
        var svc = new InventoryService(ctx, new StubClock());
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            svc.TransferAsync(new TransferRequest(productId, 10m, null), CancellationToken.None));
    }

    [Fact]
    public async Task AdjustAsync_DecreasesStockAndCreatesAdjustmentAndMovement()
    {
        var productId = await SeedProductWithStockAsync(warehouse: 15m);

        await using (var ctx = NewCtx())
        {
            var svc = new InventoryService(ctx, new StubClock());
            await svc.AdjustAsync(new AdjustmentRequest(
                productId, AdjustmentType.Loss, StockLocation.Warehouse, -3m, "Broken"),
                CancellationToken.None);
        }

        await using var verify = NewCtx();
        var p = await verify.Products.FindAsync(productId);
        Assert.NotNull(p);
        Assert.Equal(12m, p.WarehouseStock);

        var adj = await verify.InventoryAdjustments.SingleAsync(a => a.ProductId == productId);
        Assert.Equal(-3m, adj.Quantity);
        Assert.Equal(AdjustmentType.Loss, adj.Type);

        var movement = await verify.StockMovements
            .SingleAsync(m => m.ProductId == productId && m.SourceType == "Adjustment");
        Assert.Equal(-3m, movement.Quantity);
    }

    [Fact]
    public async Task AdjustAsync_ThrowsBusinessRule_WhenAdjustmentWouldMakeStockNegative()
    {
        var productId = await SeedProductWithStockAsync(warehouse: 5m);

        await using var ctx = NewCtx();
        var svc = new InventoryService(ctx, new StubClock());
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            svc.AdjustAsync(new AdjustmentRequest(
                productId, AdjustmentType.Loss, StockLocation.Warehouse, -10m, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task BatchCountAsync_SetsProductStockToCountedValues()
    {
        var productId = await SeedProductWithStockAsync(warehouse: 10m, counter: 5m);

        await using (var ctx = NewCtx())
        {
            var svc = new InventoryService(ctx, new StubClock());
            await svc.BatchCountAsync(new BatchCountRequest(
                Items: new[] { new BatchCountItem(productId, WarehouseStock: 7m, CounterStock: 3m) },
                Notes: "Daily count",
                Date: null), CancellationToken.None);
        }

        await using var verify = NewCtx();
        var p = await verify.Products.FindAsync(productId);
        Assert.NotNull(p);
        Assert.Equal(7m, p.WarehouseStock);
        Assert.Equal(3m, p.CounterStock);

        var adjustments = await verify.InventoryAdjustments
            .Where(a => a.ProductId == productId)
            .ToListAsync();
        Assert.Equal(2, adjustments.Count);

        var warehouseAdj = adjustments.Single(a => a.Location == StockLocation.Warehouse);
        Assert.Equal(-3m, warehouseAdj.Quantity);
        var counterAdj = adjustments.Single(a => a.Location == StockLocation.Counter);
        Assert.Equal(-2m, counterAdj.Quantity);
    }
}

[Collection("Postgres")]
public sealed class InventoryServiceTests_Postgres(PostgresFixture f) : InventoryServiceTestsBase
{
    protected override AppDbContext NewCtx() => f.CreateContext();
}

[Collection("Sqlite")]
public sealed class InventoryServiceTests_Sqlite(SqliteFixture f) : InventoryServiceTestsBase
{
    protected override AppDbContext NewCtx() => f.CreateContext();
}
