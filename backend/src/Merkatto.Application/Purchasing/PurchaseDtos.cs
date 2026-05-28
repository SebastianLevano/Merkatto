namespace Merkatto.Application.Purchasing;

/// <summary>One line in a purchase. Everything is in paquetes: <see cref="Paquetes"/> at
/// <see cref="CostoPorPaquete"/> each, with <see cref="UnidadesPorPaquete"/> sale units per
/// package. The product's cost and conversion factor are updated from these values.</summary>
public record CreatePurchaseItemRequest(
    long ProductId,
    decimal Paquetes,
    int UnidadesPorPaquete,
    decimal CostoPorPaquete);

public record CreatePurchaseRequest(
    string? SupplierName,
    DateOnly Date,
    string? Notes,
    IReadOnlyList<CreatePurchaseItemRequest> Items);

public record PurchaseListItem(
    long Id,
    DateOnly Date,
    string? SupplierName,
    int ItemCount,
    decimal TotalCost);

public record PurchaseItemDetail(
    long ProductId,
    string ProductName,
    string PurchaseUnit,
    decimal Quantity,
    decimal UnitCost,
    int ConversionFactor,
    decimal QuantityInSaleUnits,
    decimal Subtotal);

public record PurchaseDetail(
    long Id,
    DateOnly Date,
    long? SupplierId,
    string? SupplierName,
    string? Notes,
    decimal TotalCost,
    IReadOnlyList<PurchaseItemDetail> Items);

// --- Suppliers ---
public record SupplierItem(long Id, string Name, string? Phone, string? Notes);
public record SaveSupplierRequest(string Name, string? Phone, string? Notes);
