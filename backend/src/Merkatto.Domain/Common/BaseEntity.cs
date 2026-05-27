namespace Merkatto.Domain.Common;

/// <summary>
/// Base for all persistent entities. Single-tenant: no TenantId by design.
/// Identity uses bigint sequences (simple, index-friendly).
/// </summary>
public abstract class BaseEntity : IAuditable, ISoftDelete
{
    public long Id { get; set; }

    // IAuditable
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // ISoftDelete
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
