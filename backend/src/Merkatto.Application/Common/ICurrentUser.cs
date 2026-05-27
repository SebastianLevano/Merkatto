namespace Merkatto.Application.Common;

/// <summary>Ambient info about the authenticated user, resolved per request.</summary>
public interface ICurrentUser
{
    long? UserId { get; }
    string? Email { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}
