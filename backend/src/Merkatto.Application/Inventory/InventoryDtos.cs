using Merkatto.Domain.Inventory;

namespace Merkatto.Application.Inventory;

public record InventoryRow(
    long ProductId,
    string Name,
    string SaleUnit,
    decimal WarehouseStock,
    decimal CounterStock,
    decimal TotalStock,
    decimal MinStock,
    bool IsLowStock,
    decimal? DaysOfStock);

/// <summary>Move stock from warehouse to counter (the "abrir mercadería" action).</summary>
public record TransferRequest(long ProductId, decimal Quantity, string? Notes);

/// <summary>
/// Adjust a location's stock. <see cref="Quantity"/> is signed: negative reduces (losses,
/// expired, internal use), positive increases (corrections that add).
/// </summary>
public record AdjustmentRequest(
    long ProductId,
    AdjustmentType Type,
    StockLocation Location,
    decimal Quantity,
    string? Reason);

public record BatchCountItem(long ProductId, decimal WarehouseStock, decimal CounterStock);

public record BatchCountRequest(IReadOnlyList<BatchCountItem> Items, string? Notes);

public record MovementRow(
    long Id,
    long ProductId,
    string ProductName,
    DateTimeOffset OccurredAt,
    MovementType MovementType,
    StockLocation Location,
    decimal Quantity,
    string? Notes);
