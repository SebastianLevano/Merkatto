using Merkatto.Domain.Auth;

namespace Merkatto.Application.Auth;

/// <summary>
/// Talks to the central identity server (the VPS). Implemented in Infrastructure (HTTP) and
/// registered only in the desktop host of a bodega that has a central configured. When this is
/// not registered (cloud server, standalone install), <see cref="AuthService"/> falls back to
/// pure local authentication.
/// </summary>
public interface ICentralAuthClient
{
    /// <summary>
    /// Validates credentials against the central server.
    /// Returns the user's authoritative identity on success;
    /// returns <c>null</c> when the central could not be reached (offline / transient 5xx),
    /// so the caller should fall back to the local credential cache;
    /// throws <see cref="CentralRejectedException"/> when the central actively rejected the
    /// credentials (the caller must NOT fall back to cache in that case).
    /// </summary>
    Task<CentralLoginResult?> ValidateAsync(string email, string password, CancellationToken ct);

    /// <summary>
    /// Changes the user's password on the central (the source of truth). Throws on failure;
    /// the caller only updates the local cache after this succeeds.
    /// </summary>
    Task ChangePasswordAsync(string email, string currentPassword, string newPassword, CancellationToken ct);
}

public sealed record CentralLoginResult(
    string Email,
    string FullName,
    Role Role,
    bool MustChangePassword,
    bool IsActive,
    string? BusinessName);

/// <summary>The central reached us and said the credentials are invalid. Do not use the cache.</summary>
public sealed class CentralRejectedException(string message) : Exception(message);

/// <summary>The central could not be reached (offline / transient failure).</summary>
public sealed class CentralUnavailableException(string message) : Exception(message);
