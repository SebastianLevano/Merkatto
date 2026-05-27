namespace Merkatto.Domain.Common;

/// <summary>Tracks who created/changed a record and when. Filled by the EF SaveChanges interceptor.</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
