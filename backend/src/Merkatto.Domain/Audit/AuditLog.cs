namespace Merkatto.Domain.Audit;

/// <summary>
/// Immutable record of a data change (who, what, when). Written by the EF SaveChanges
/// interceptor; also feeds the operational timeline. Not a BaseEntity (never soft-deleted/edited).
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public required string Action { get; set; }       // Created | Updated | Deleted
    public required string EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }            // JSON
    public string? NewValues { get; set; }            // JSON
    public DateTimeOffset Timestamp { get; set; }
    public string? IpAddress { get; set; }
}
