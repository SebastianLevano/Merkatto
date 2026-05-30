using Merkatto.Domain.Auth;

namespace Merkatto.Application.Users;

public record UserListItem(
    long Id,
    string Email,
    string FullName,
    Role Role,
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset? LastLoginAt);

public record CreateUserRequest(string Email, string FullName, Role Role, string Password);

public record UpdateUserRequest(string FullName, Role Role, bool IsActive);

public record ResetPasswordRequest(string Password);
