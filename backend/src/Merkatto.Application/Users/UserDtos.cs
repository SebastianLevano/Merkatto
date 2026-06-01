using Merkatto.Domain.Auth;

namespace Merkatto.Application.Users;

public record UserListItem(
    long Id,
    string Email,
    string FullName,
    Role Role,
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset? LastLoginAt,
    string? BusinessName);

public record CreateUserRequest(string Email, string FullName, Role Role, string Password, string? BusinessName);

public record UpdateUserRequest(string FullName, Role Role, bool IsActive, string? BusinessName);

public record ResetPasswordRequest(string Password);
