using Merkatto.Domain.Catalog;
using Merkatto.Domain.Common;

namespace Merkatto.Domain.Purchasing;

/// <summary>
/// A line of a purchase. Quantity is expressed in <see cref="PurchaseUnit"/>; the conversion
/// factor at the time of purchase is captured so historical math stays correct even if the
/// product's configuration later changes.
/// </summary>
public class PurchaseItem : BaseEntity
{
    public long PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public required string PurchaseUnit { get; set; }
    public decimal Quantity { get; set; }
    public int ConversionFactorSnapshot { get; set; } = 1;
    public decimal UnitCostSnapshot { get; set; }

    public decimal Subtotal => Quantity * UnitCostSnapshot;
    /// <summary>Quantity converted to sale (base) units, added to inventory.</summary>
    public decimal QuantityInSaleUnits => Quantity * ConversionFactorSnapshot;
}
