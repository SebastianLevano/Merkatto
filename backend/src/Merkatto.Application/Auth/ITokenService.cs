using Merkatto.Domain.Auth;

namespace Merkatto.Application.Auth;

public interface ITokenService
{
    /// <summary>Creates a signed JWT access token for the user.</summary>
    (string token, DateTimeOffset expiresAt) CreateAccessToken(User user);

    /// <summary>Generates a cryptographically random refresh token (raw value).</summary>
    string CreateRefreshToken();

    /// <summary>Hashes a raw refresh token for storage / comparison.</summary>
    string HashRefreshToken(string rawToken);
}
