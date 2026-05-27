using Merkatto.Domain.Catalog;
using Merkatto.Domain.Common;

namespace Merkatto.Domain.Inventory;

public enum AdjustmentType
{
    Loss = 1,
    Expired = 2,
    InternalUse = 3,
    Correction = 4
}

/// <summary>Manual inventory adjustment (loss, expired, internal use, correction).</summary>
public class InventoryAdjustment : BaseEntity
{
    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public AdjustmentType Type { get; set; }
    public StockLocation Location { get; set; }
    /// <summary>Signed quantity in sale units (negative for losses/consumption).</summary>
    public decimal Quantity { get; set; }
    public string? Reason { get; set; }
    public DateOnly Date { get; set; }
}
