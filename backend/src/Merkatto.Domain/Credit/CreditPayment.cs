using Merkatto.Domain.Common;

namespace Merkatto.Domain.Credit;

public class CreditPayment : BaseEntity
{
    public long CustomerId { get; set; }
    public CreditCustomer Customer { get; set; } = null!;

    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
