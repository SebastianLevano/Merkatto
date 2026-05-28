namespace Merkatto.Application.Catalog;

/// <summary>Compact product row for lists (includes derived cost/margin, stock flags and rotation estimate).</summary>
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
    decimal WarehouseStock,
    decimal CounterStock,
    decimal TotalStock,
    decimal MinStock,
    bool IsLowStock,
    bool IsActive,
    decimal? DaysOfStock);

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

/// <summary>
/// Costs and units per package come from purchases (or from the optional initial-load block).
/// The purchase unit is always "paquete"; the form doesn't ask for it.
/// </summary>
public record CreateProductRequest(
    string Name,
    string? InternalCode,
    long CategoryId,
    long? BrandId,
    string SaleUnit,
    decimal SalePrice,
    decimal MinStock,
    // Optional "ya tengo este producto en stock" block. All three together or all empty.
    int? InitialPaquetes,
    int? InitialUnidadesPorPaquete,
    decimal? InitialCostoPorPaquete);

public record UpdateProductRequest(
    string Name,
    string? InternalCode,
    long CategoryId,
    long? BrandId,
    string SaleUnit,
    decimal SalePrice,
    decimal MinStock);
