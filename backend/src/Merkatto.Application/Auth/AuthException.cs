namespace Merkatto.Application.Auth;

/// <summary>Raised for authentication failures (bad credentials, invalid/expired refresh token).</summary>
public sealed class AuthException(string message) : Exception(message);
