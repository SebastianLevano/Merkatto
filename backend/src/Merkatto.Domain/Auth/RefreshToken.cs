using Merkatto.Domain.Common;

namespace Merkatto.Domain.Auth;

/// <summary>
/// Rotating refresh token. Only the hash is stored. Reuse of a revoked token is detectable
/// via <see cref="ReplacedByTokenHash"/>.
/// </summary>
public class RefreshToken : BaseEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; set; }

    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}
