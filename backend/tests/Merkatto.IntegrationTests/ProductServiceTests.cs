using Merkatto.Application.Catalog;
using Merkatto.Application.Common;
using Merkatto.Domain.Catalog;
using Merkatto.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.IntegrationTests;

[Collection("Database")]
public class ProductServiceTests(DatabaseFixture db)
{
    private async Task<long> SeedCategoryAsync()
    {
        await using var ctx = db.CreateContext();
        var cat = new Category { Name = $"Cat-{Guid.NewGuid():N}" };
        ctx.Categories.Add(cat);
        await ctx.SaveChangesAsync();
        return cat.Id;
    }

    [Fact]
    public async Task CreateAsync_WithoutInitialStock_CreatesProductWithZeroStockAndNoMovements()
    {
        var catId = await SeedCategoryAsync();

        long id;
        await using (var ctx = db.CreateContext())
        {
            var svc = new ProductService(ctx, new StubClock());
            id = await svc.CreateAsync(new CreateProductRequest(
                $"Galleta-{Guid.NewGuid():N}", null, catId, null, "unidad", 1.50m, 5,
                null, null, null), CancellationToken.None);
        }

        await using var verify = db.CreateContext();
        var p = await verify.Products.FindAsync(id);
        Assert.NotNull(p);
        Assert.Equal(0m, p.WarehouseStock);
        Assert.Equal("paquete", p.PurchaseUnit);
        Assert.Empty(await verify.StockMovements.Where(m => m.ProductId == id).ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WithInitialStock_SetsStockAndCreatesInitialStockMovement()
    {
        var catId = await SeedCategoryAsync();

        long id;
        await using (var ctx = db.CreateContext())
        {
            var svc = new ProductService(ctx, new StubClock());
            id = await svc.CreateAsync(new CreateProductRequest(
                $"Leche-{Guid.NewGuid():N}", null, catId, null, "unidad", 4m, 6,
                InitialPaquetes: 2, InitialUnidadesPorPaquete: 6, InitialCostoPorPaquete: 18m),
                CancellationToken.None);
        }

        await using var verify = db.CreateContext();
        var p = await verify.Products.FindAsync(id);
        Assert.NotNull(p);
        Assert.Equal(12m, p.WarehouseStock);      // 2 paquetes × 6 unidades
        Assert.Equal(18m, p.LastPurchaseCost);
        Assert.Equal(6, p.UnitsPerPurchaseUnit);

        var movement = await verify.StockMovements.SingleAsync(m => m.ProductId == id);
        Assert.Equal(12m, movement.Quantity);
        Assert.Equal("InitialStock", movement.SourceType);
    }

    [Fact]
    public async Task CreateAsync_ThrowsNotFoundException_WhenCategoryDoesNotExist()
    {
        await using var ctx = db.CreateContext();
        var svc = new ProductService(ctx, new StubClock());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.CreateAsync(new CreateProductRequest(
                "Ghost", null, 999_999L, null, "unidad", 1m, 0, null, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_ProductHiddenButStillInDb()
    {
        var catId = await SeedCategoryAsync();

        long id;
        await using (var ctx = db.CreateContext())
        {
            var svc = new ProductService(ctx, new StubClock());
            id = await svc.CreateAsync(new CreateProductRequest(
                $"ToDelete-{Guid.NewGuid():N}", null, catId, null, "unidad", 1m, 0,
                null, null, null), CancellationToken.None);
        }

        await using (var ctx = db.CreateContext())
        {
            var svc = new ProductService(ctx, new StubClock());
            await svc.DeleteAsync(id, CancellationToken.None);
        }

        await using var verify = db.CreateContext();
        Assert.Null(await verify.Products.FindAsync(id)); // hidden by global filter

        var raw = await verify.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
        Assert.NotNull(raw);
        Assert.True(raw.IsDeleted);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEditableFields_AndLeavesLastPurchaseCostUntouched()
    {
        var catId = await SeedCategoryAsync();

        long id;
        await using (var ctx = db.CreateContext())
        {
            var svc = new ProductService(ctx, new StubClock());
            id = await svc.CreateAsync(new CreateProductRequest(
                $"Prod-{Guid.NewGuid():N}", null, catId, null, "unidad", 1.50m, 5,
                InitialPaquetes: 1, InitialUnidadesPorPaquete: 12, InitialCostoPorPaquete: 6m),
                CancellationToken.None);
        }

        await using (var ctx = db.CreateContext())
        {
            var svc = new ProductService(ctx, new StubClock());
            await svc.UpdateAsync(id, new UpdateProductRequest(
                "Nombre Actualizado", null, catId, null, "unidad", 2.00m, 10),
                CancellationToken.None);
        }

        await using var verify = db.CreateContext();
        var p = await verify.Products.FindAsync(id);
        Assert.NotNull(p);
        Assert.Equal("Nombre Actualizado", p.Name);
        Assert.Equal(2.00m, p.SalePrice);
        // Cost and units/paquete only update via purchases, not via product edit.
        Assert.Equal(6m, p.LastPurchaseCost);
        Assert.Equal(12, p.UnitsPerPurchaseUnit);
    }
}
