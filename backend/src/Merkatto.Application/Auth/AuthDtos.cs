using Merkatto.Domain.Auth;

namespace Merkatto.Application.Auth;

public record LoginRequest(string Email, string Password);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Access token returned to the client (kept in memory). The refresh token travels in an httpOnly cookie.</summary>
public record AuthResult(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, UserDto User);

public record UserDto(long Id, string Email, string FullName, Role Role);

/// <summary>Issued JWT plus its raw refresh token (the caller sets the cookie) and the user.</summary>
public record TokenPair(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserDto User);
