namespace Merkatto.Application.Audit;

public record TimelineEntry(
    long Id,
    string Action,
    string EntityName,
    string? EntityId,
    string UserDisplay,
    DateTimeOffset Timestamp,
    string? IpAddress);
