using Merkatto.Application.Common;
using Merkatto.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Merkatto.Application.Auth;

/// <summary>
/// Login, refresh-token rotation (with reuse detection), and logout. The raw refresh token is
/// returned to the caller, which stores it in an httpOnly cookie; only its hash is persisted.
/// </summary>
public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokens,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IOptions<AuthSettings> settings)
{
    private readonly AuthSettings _settings = settings.Value;

    public async Task<TokenPair> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower(), ct);
        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new AuthException("Credenciales inválidas.");

        user.LastLoginAt = clock.UtcNow;
        var pair = await IssueTokensAsync(user, ct);
        await db.SaveChangesAsync(ct);
        return pair;
    }

    public async Task<TokenPair> RefreshAsync(string rawRefreshToken, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(rawRefreshToken);
        var stored = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct)
            ?? throw new AuthException("Token de refresco inválido.");

        if (stored.RevokedAt is not null)
        {
            // Reuse of an already-rotated token: revoke the whole chain for safety.
            await RevokeAllForUserAsync(stored.UserId, ct);
            await db.SaveChangesAsync(ct);
            throw new AuthException("Token de refresco reutilizado. Sesión revocada.");
        }

        if (!stored.IsActive)
            throw new AuthException("Token de refresco expirado.");

        var pair = await IssueTokensAsync(stored.User, ct);
        stored.RevokedAt = clock.UtcNow;
        stored.ReplacedByTokenHash = tokens.HashRefreshToken(pair.RefreshToken);
        await db.SaveChangesAsync(ct);
        return pair;
    }

    public async Task LogoutAsync(string rawRefreshToken, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(rawRefreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);
        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = clock.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<UserDto?> GetCurrentAsync(CancellationToken ct)
    {
        if (currentUser.UserId is not { } id) return null;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        return user is null ? null : new UserDto(user.Id, user.Email, user.FullName, user.Role);
    }

    private async Task<TokenPair> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (access, accessExp) = tokens.CreateAccessToken(user);
        var raw = tokens.CreateRefreshToken();
        var refreshExp = clock.UtcNow.AddDays(_settings.RefreshTokenDays);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokens.HashRefreshToken(raw),
            ExpiresAt = refreshExp,
            CreatedByIp = currentUser.IpAddress
        });

        await Task.CompletedTask;
        var dto = new UserDto(user.Id, user.Email, user.FullName, user.Role);
        return new TokenPair(access, accessExp, raw, refreshExp, dto);
    }

    private async Task RevokeAllForUserAsync(long userId, CancellationToken ct)
    {
        var active = await db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in active) t.RevokedAt = clock.UtcNow;
    }
}
