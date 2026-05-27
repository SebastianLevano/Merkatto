using Merkatto.Domain.Common;

namespace Merkatto.Domain.Purchasing;

public class Supplier : BaseEntity
{
    public required string Name { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }

    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
