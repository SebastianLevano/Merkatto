namespace Merkatto.Application.Purchasing;

public record CreatePurchaseItemRequest(long ProductId, decimal Quantity, decimal UnitCost);

public record CreatePurchaseRequest(
    long? SupplierId,
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
