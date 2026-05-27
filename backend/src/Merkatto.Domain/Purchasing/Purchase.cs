using Merkatto.Domain.Common;

namespace Merkatto.Domain.Purchasing;

/// <summary>A purchase from a supplier. Registering its items increases warehouse stock.</summary>
public class Purchase : BaseEntity
{
    public long? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public DateOnly Date { get; set; }
    public decimal TotalCost { get; set; }
    public string? Notes { get; set; }

    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}
