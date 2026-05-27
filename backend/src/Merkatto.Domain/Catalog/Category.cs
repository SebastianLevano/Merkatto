using Merkatto.Domain.Common;

namespace Merkatto.Domain.Catalog;

public class Category : BaseEntity
{
    public required string Name { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
