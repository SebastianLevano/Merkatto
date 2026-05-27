using Merkatto.Domain.Catalog;
using Xunit;

namespace Merkatto.UnitTests;

public class ProductMathTests
{
    // Real example: galleta soda — 10 paquetes a S/6, 6 unidades por paquete, venta a S/1.50.
    private static Product GalletaSoda() => new()
    {
        Name = "Galleta Soda",
        PurchaseUnit = "paquete",
        SaleUnit = "unidad",
        LastPurchaseCost = 6m,
        UnitsPerPurchaseUnit = 6,
        SalePrice = 1.50m
    };

    [Fact]
    public void UnitCost_DividesPurchaseCostByConversionFactor()
    {
        Assert.Equal(1.00m, GalletaSoda().UnitCost);
    }

    [Fact]
    public void Margin_IsSalePriceMinusUnitCost()
    {
        Assert.Equal(0.50m, GalletaSoda().Margin);
    }

    [Fact]
    public void MarginRate_IsMarginOverSalePrice()
    {
        Assert.Equal(1m / 3m, GalletaSoda().MarginRate, precision: 6);
    }

    [Fact]
    public void UnitCost_IsZero_WhenConversionFactorInvalid()
    {
        var p = GalletaSoda();
        p.UnitsPerPurchaseUnit = 0;
        Assert.Equal(0m, p.UnitCost);
    }

    [Theory]
    [InlineData(5, 3, false)]   // 8 total > 3 min
    [InlineData(1, 1, false)]   // 2 total > 1 min
    [InlineData(2, 0, true)]    // 2 total <= 2 min
    public void IsLowStock_ComparesTotalStockToMin(decimal warehouse, decimal counter, bool _)
    {
        var p = GalletaSoda();
        p.WarehouseStock = warehouse;
        p.CounterStock = counter;
        p.MinStock = 2m;
        Assert.Equal(p.TotalStock <= p.MinStock, p.IsLowStock);
    }
}
