using Merkatto.Application.Common;
using Merkatto.Domain.Auth;
using Merkatto.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Merkatto.Application.Auth;

/// <summary>
/// Login, refresh-token rotation (with reuse detection), and logout. The raw refresh token is
/// returned to the caller, which stores it in an httpOnly cookie; only its hash is persisted.
///
/// When a central identity server is configured (a bodega desktop install), login validates
/// against it and caches the credential locally for offline use; otherwise (cloud server,
/// standalone install) authentication is purely local. The optional dependency is taken as an
/// enumerable so DI resolves an empty set when no central client is registered.
/// </summary>
public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokens,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IOptions<AuthSettings> settings,
    IEnumerable<ICentralAuthClient> centralClients)
{
    private readonly AuthSettings _settings = settings.Value;
    private readonly ICentralAuthClient? _central = centralClients.FirstOrDefault();

    public async Task<TokenPair> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLower();

        // Bodega install with a central configured: validate against the source of truth.
        if (_central is not null)
        {
            var result = await ValidateAgainstCentralAsync(email, request.Password, ct);
            if (result is not null)
            {
                var cached = await UpsertCachedUserAsync(result, request.Password, ct);
                cached.LastLoginAt = clock.UtcNow;
                var centralPair = await IssueTokensAsync(cached, ct);
                await db.SaveChangesAsync(ct);
                return centralPair;
            }
            // result == null => central unreachable: fall through to the local credential cache.
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new AuthException("Credenciales inválidas.");

        user.LastLoginAt = clock.UtcNow;
        var pair = await IssueTokensAsync(user, ct);
        await db.SaveChangesAsync(ct);
        return pair;
    }

    /// <summary>
    /// Calls the central. Returns the authoritative identity on success, or null when the central
    /// is unreachable (caller falls back to cache). Throws <see cref="AuthException"/> (401) when
    /// the central rejects the credentials, the account is disabled, the role is Administrator
    /// (admins are managed from the admin console, not a bodega install), or this install is
    /// already bound to a different Encargado.
    /// </summary>
    private async Task<CentralLoginResult?> ValidateAgainstCentralAsync(string email, string password, CancellationToken ct)
    {
        CentralLoginResult? result;
        try
        {
            result = await _central!.ValidateAsync(email, password, ct);
        }
        catch (CentralRejectedException)
        {
            throw new AuthException("Credenciales inválidas.");
        }

        if (result is null) return null; // offline

        if (!result.IsActive)
            throw new AuthException("Tu cuenta está desactivada. Contactá al administrador del sistema.");

        if (result.Role == Role.Administrator)
            throw new AuthException("Las cuentas de administrador se gestionan desde la consola de administración, no desde la app de la bodega.");

        await EnsureInstallBindingAsync(result.Email, ct);
        return result;
    }

    /// <summary>
    /// Ties this install to the first Encargado that logs in. A different account is rejected so a
    /// bodega's local data is never exposed to another user.
    /// </summary>
    private async Task EnsureInstallBindingAsync(string email, CancellationToken ct)
    {
        var bound = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == AppSettingKeys.BoundUserEmail, ct);
        if (bound is null)
            db.AppSettings.Add(new AppSetting { Key = AppSettingKeys.BoundUserEmail, Value = email });
        else if (!string.Equals(bound.Value, email, StringComparison.OrdinalIgnoreCase))
            throw new AuthException("Esta instalación pertenece a otra bodega. Usá la instalación de tu propio negocio.");
    }

    /// <summary>
    /// Caches/refreshes the central-validated user locally so the bodega can log in offline next
    /// time. The password is hashed locally from the plaintext in this request — the central's own
    /// hash is never transmitted or stored. Also mirrors the business name into the local setting.
    /// </summary>
    private async Task<User> UpsertCachedUserAsync(CentralLoginResult result, string plainPassword, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == result.Email, ct);
        if (user is null)
        {
            user = new User
            {
                Email = result.Email,
                FullName = result.FullName,
                PasswordHash = passwordHasher.Hash(plainPassword),
                Role = result.Role,
                IsActive = true,
                MustChangePassword = result.MustChangePassword,
                BusinessName = result.BusinessName,
                CreatedAt = clock.UtcNow,
                CreatedBy = "central"
            };
            db.Users.Add(user);
        }
        else
        {
            user.FullName = result.FullName;
            user.PasswordHash = passwordHasher.Hash(plainPassword); // re-cache (picks up remote password changes)
            user.Role = result.Role;
            user.IsActive = true;
            user.MustChangePassword = result.MustChangePassword;
            user.BusinessName = result.BusinessName;
        }

        if (!string.IsNullOrWhiteSpace(result.BusinessName))
        {
            var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == AppSettingKeys.BusinessName, ct);
            if (setting is null)
                db.AppSettings.Add(new AppSetting { Key = AppSettingKeys.BusinessName, Value = result.BusinessName });
            else
                setting.Value = result.BusinessName;
        }

        return user;
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
        return user is null ? null : new UserDto(user.Id, user.Email, user.FullName, user.Role, user.MustChangePassword, user.BusinessName);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } id)
            throw new AuthException("No autenticado.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new BusinessRuleException("La nueva contraseña debe tener al menos 8 caracteres.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new AuthException("Usuario no encontrado.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new AuthException("La contraseña actual es incorrecta.");

        // When a central is configured, the password lives in the source of truth: change it there
        // first, then update the local cache. Offline, the change is blocked (it would diverge).
        if (_central is not null)
        {
            try
            {
                await _central.ChangePasswordAsync(user.Email, request.CurrentPassword, request.NewPassword, ct);
            }
            catch (CentralRejectedException ex)
            {
                throw new AuthException(ex.Message);
            }
            catch (CentralUnavailableException)
            {
                throw new BusinessRuleException("Necesitás conexión a internet para cambiar tu contraseña.");
            }
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        await db.SaveChangesAsync(ct);
    }

    private async Task<TokenPair> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (access, accessExp) = tokens.CreateAccessToken(user);
        var raw = tokens.CreateRefreshToken();
        var refreshExp = clock.UtcNow.AddDays(_settings.RefreshTokenDays);

        db.RefreshTokens.Add(new RefreshToken
        {
            // Use the navigation rather than UserId so EF resolves the FK even when the user was
            // just created in this same unit of work (broker first-login caching) and has no Id yet.
            User = user,
            TokenHash = tokens.HashRefreshToken(raw),
            ExpiresAt = refreshExp,
            CreatedByIp = currentUser.IpAddress
        });

        await Task.CompletedTask;
        var dto = new UserDto(user.Id, user.Email, user.FullName, user.Role, user.MustChangePassword, user.BusinessName);
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
