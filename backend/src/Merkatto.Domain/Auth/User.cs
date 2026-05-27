using Merkatto.Domain.Common;

namespace Merkatto.Domain.Auth;

public class User : BaseEntity
{
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public required string PasswordHash { get; set; }
    public Role Role { get; set; } = Role.Collaborator;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
