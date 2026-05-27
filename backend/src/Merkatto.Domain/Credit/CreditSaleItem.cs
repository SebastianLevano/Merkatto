using Merkatto.Domain.Common;

namespace Merkatto.Domain.Credit;

public class CreditSaleItem : BaseEntity
{
    public long CreditSaleId { get; set; }
    public CreditSale CreditSale { get; set; } = null!;

    public required string Description { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal LineTotal { get; set; }
}
