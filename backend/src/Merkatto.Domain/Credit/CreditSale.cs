using Merkatto.Domain.Common;

namespace Merkatto.Domain.Credit;

/// <summary>A credit sale. Items are free-text lines (e.g. "Pan x4") — fast, no product lookup required.</summary>
public class CreditSale : BaseEntity
{
    public long CustomerId { get; set; }
    public CreditCustomer Customer { get; set; } = null!;

    public DateOnly Date { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }

    public ICollection<CreditSaleItem> Items { get; set; } = new List<CreditSaleItem>();
}
