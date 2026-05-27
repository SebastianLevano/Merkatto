namespace Merkatto.Application.Catalog;

/// <summary>Compact product row for lists (includes derived cost/margin and stock flags).</summary>
public record ProductListItem(
    long Id,
    string Name,
    string? InternalCode,
    string CategoryName,
    string? BrandName,
    string SaleUnit,
    decimal SalePrice,
    decimal UnitCost,
    decimal Margin,
    decimal MarginRate,
    decimal TotalStock,
    decimal MinStock,
    bool IsLowStock,
    bool IsActive);

/// <summary>Full product detail for the edit form.</summary>
public record ProductDetail(
    long Id,
    string Name,
    string? InternalCode,
    long CategoryId,
    string CategoryName,
    long? BrandId,
    string? BrandName,
    string PurchaseUnit,
    decimal LastPurchaseCost,
    int UnitsPerPurchaseUnit,
    string SaleUnit,
    decimal SalePrice,
    decimal WarehouseStock,
    decimal CounterStock,
    decimal MinStock,
    decimal UnitCost,
    decimal Margin,
    decimal MarginRate,
    bool IsActive);

public record CreateProductRequest(
    string Name,
    string? InternalCode,
    long CategoryId,
    long? BrandId,
    string PurchaseUnit,
    decimal LastPurchaseCost,
    int UnitsPerPurchaseUnit,
    string SaleUnit,
    decimal SalePrice,
    decimal MinStock,
    decimal InitialWarehouseStock);

public record UpdateProductRequest(
    string Name,
    string? InternalCode,
    long CategoryId,
    long? BrandId,
    string PurchaseUnit,
    decimal LastPurchaseCost,
    int UnitsPerPurchaseUnit,
    string SaleUnit,
    decimal SalePrice,
    decimal MinStock);
