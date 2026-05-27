namespace Merkatto.Application.Common;

/// <summary>Abstracts the clock so time-dependent logic is testable.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }
}
