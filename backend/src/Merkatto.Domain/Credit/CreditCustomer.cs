using Merkatto.Domain.Common;

namespace Merkatto.Domain.Credit;

/// <summary>A trusted customer who buys on credit ("fiado"). Minimal data on purpose.</summary>
public class CreditCustomer : BaseEntity
{
    public required string Name { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }

    public ICollection<CreditSale> Sales { get; set; } = new List<CreditSale>();
    public ICollection<CreditPayment> Payments { get; set; } = new List<CreditPayment>();
}
