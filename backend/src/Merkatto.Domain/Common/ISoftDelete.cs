namespace Merkatto.Domain.Common;

/// <summary>Marks an entity as soft-deletable. A global query filter hides deleted rows.</summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}
