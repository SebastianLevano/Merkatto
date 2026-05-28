using Merkatto.Application.Common;

namespace Merkatto.IntegrationTests.Infrastructure;

internal sealed class StubCurrentUser : ICurrentUser
{
    public long? UserId => 1;
    public string? Email => "test@merkatto.pe";
    public string? IpAddress => "127.0.0.1";
    public bool IsAuthenticated => true;
}

internal sealed class StubClock : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
    public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}
