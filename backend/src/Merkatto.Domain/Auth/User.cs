using Merkatto.Domain.Common;

namespace Merkatto.Domain.Auth;

public class User : BaseEntity
{
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public required string PasswordHash { get; set; }
    public Role Role { get; set; } = Role.Collaborator;
    public bool IsActive { get; set; } = true;

    /// <summary>When true, the user must set a new password before using the rest of the app.
    /// Set on seeded admins and on any user created/reset by an administrator.</summary>
    public bool MustChangePassword { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
