using Merkatto.Domain.Catalog;
using Merkatto.Domain.Common;

namespace Merkatto.Domain.Inventory;

public enum MovementType
{
    Purchase = 1,
    Transfer = 2,
    Adjustment = 3,
    EstimatedSale = 4,
    InternalUse = 5
}

public enum StockLocation
{
    Warehouse = 1,
    Counter = 2
}

/// <summary>
/// Append-only ledger of every stock change (in sale units). Quantity is signed:
/// positive adds, negative removes. Backs traceability and rotation estimates.
/// </summary>
public class StockMovement : BaseEntity
{
    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public MovementType MovementType { get; set; }
    public StockLocation Location { get; set; }
    public decimal Quantity { get; set; }

    public string? SourceType { get; set; }
    public long? SourceId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? Notes { get; set; }
}
