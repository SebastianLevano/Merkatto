using Merkatto.Application.Auth;
using Merkatto.Application.Common;
using Merkatto.Domain.Auth;
using Merkatto.Domain.Common;
using Merkatto.Infrastructure.Persistence;
using Merkatto.Infrastructure.Security;
using Merkatto.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Merkatto.IntegrationTests;

/// <summary>
/// Verifies the central-auth broker in <see cref="AuthService"/>: online validation caches the
/// credential locally, offline falls back to that cache, a central rejection is final, and an
/// install is bound to a single Encargado (no admins, no other accounts).
/// </summary>
[Collection("Sqlite")]
public sealed class AuthBrokerTests(SqliteFixture fixture)
{
    private static readonly AuthSettings Settings = new()
    {
        Issuer = "Merkatto",
        Audience = "Merkatto",
        SigningKey = "test-signing-key-that-is-definitely-long-enough-1234567890",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    };

    private AppDbContext NewCtx() => fixture.CreateContext();

    /// <summary>
    /// The SQLite fixture shares one in-memory DB across the collection, and install binding +
    /// business name are singleton rows. Clear them (and cached users) so each test starts from a
    /// clean, unbound install.
    /// </summary>
    private async Task ResetInstallAsync()
    {
        await using var ctx = NewCtx();
        await ctx.RefreshTokens.ExecuteDeleteAsync();
        await ctx.Users.ExecuteDeleteAsync();
        await ctx.AppSettings
            .Where(s => s.Key == AppSettingKeys.BoundUserEmail || s.Key == AppSettingKeys.BusinessName)
            .ExecuteDeleteAsync();
    }

    private static AuthService NewService(AppDbContext ctx, ICentralAuthClient? central)
    {
        var hasher = new Argon2PasswordHasher();
        var tokens = new JwtTokenService(Options.Create(Settings));
        var clients = central is null ? Array.Empty<ICentralAuthClient>() : [central];
        return new AuthService(ctx, hasher, tokens, new StubClock(), new StubCurrentUser(),
            Options.Create(Settings), clients);
    }

    private async Task<string?> GetSettingAsync(string key)
    {
        await using var ctx = NewCtx();
        return await ctx.AppSettings.Where(s => s.Key == key).Select(s => s.Value).FirstOrDefaultAsync();
    }

    [Fact]
    public async Task Login_OnlineSuccess_CachesUserBindsInstallAndMirrorsBusinessName()
    {
        await ResetInstallAsync();
        var email = $"rosa-{Guid.NewGuid():N}@bodega.pe";
        var central = new StubCentral
        {
            Result = new CentralLoginResult(email, "Rosa", Role.Encargado, false, true, "Bodega Doña Rosa")
        };

        await using (var ctx = NewCtx())
        {
            var svc = NewService(ctx, central);
            var pair = await svc.LoginAsync(new LoginRequest(email, "secret123"), CancellationToken.None);
            Assert.Equal(email, pair.User.Email);
            Assert.Equal("Bodega Doña Rosa", pair.User.BusinessName);
        }

        await using var verify = NewCtx();
        var cached = await verify.Users.FirstOrDefaultAsync(u => u.Email == email);
        Assert.NotNull(cached);
        Assert.Equal(Role.Encargado, cached!.Role);
        Assert.NotEqual("secret123", cached.PasswordHash); // hashed locally, not the plaintext
        Assert.Equal(email, await GetSettingAsync(AppSettingKeys.BoundUserEmail));
        Assert.Equal("Bodega Doña Rosa", await GetSettingAsync(AppSettingKeys.BusinessName));
    }

    [Fact]
    public async Task Login_OfflineAfterPriorOnlineLogin_UsesLocalCache()
    {
        await ResetInstallAsync();
        var email = $"caché-{Guid.NewGuid():N}@bodega.pe";
        var central = new StubCentral
        {
            Result = new CentralLoginResult(email, "Caché", Role.Encargado, false, true, "Mi Bodega")
        };

        // First login online to populate the cache.
        await using (var ctx = NewCtx())
            await NewService(ctx, central).LoginAsync(new LoginRequest(email, "secret123"), CancellationToken.None);

        // Central now unreachable (ValidateAsync returns null).
        central.Result = null;
        central.Offline = true;

        await using var ctx2 = NewCtx();
        var pair = await NewService(ctx2, central).LoginAsync(new LoginRequest(email, "secret123"), CancellationToken.None);
        Assert.Equal(email, pair.User.Email);
    }

    [Fact]
    public async Task Login_OfflineWithWrongPassword_Fails()
    {
        await ResetInstallAsync();
        var email = $"caché2-{Guid.NewGuid():N}@bodega.pe";
        var central = new StubCentral
        {
            Result = new CentralLoginResult(email, "Caché", Role.Encargado, false, true, "Mi Bodega")
        };
        await using (var ctx = NewCtx())
            await NewService(ctx, central).LoginAsync(new LoginRequest(email, "secret123"), CancellationToken.None);

        central.Result = null;
        central.Offline = true;

        await using var ctx2 = NewCtx();
        var svc = NewService(ctx2, central);
        await Assert.ThrowsAsync<AuthException>(() =>
            svc.LoginAsync(new LoginRequest(email, "wrong-password"), CancellationToken.None));
    }

    [Fact]
    public async Task Login_CentralRejects_DoesNotFallBackToCache()
    {
        await ResetInstallAsync();
        var email = $"reject-{Guid.NewGuid():N}@bodega.pe";
        var central = new StubCentral
        {
            Result = new CentralLoginResult(email, "X", Role.Encargado, false, true, "B")
        };
        // Seed a local cache so a fallback WOULD succeed if (incorrectly) attempted.
        await using (var ctx = NewCtx())
            await NewService(ctx, central).LoginAsync(new LoginRequest(email, "secret123"), CancellationToken.None);

        central.RejectAll = true;
        await using var ctx2 = NewCtx();
        var svc = NewService(ctx2, central);
        await Assert.ThrowsAsync<AuthException>(() =>
            svc.LoginAsync(new LoginRequest(email, "secret123"), CancellationToken.None));
    }

    [Fact]
    public async Task Login_DifferentEncargadoOnBoundInstall_IsRejected()
    {
        await ResetInstallAsync();
        var first = $"first-{Guid.NewGuid():N}@bodega.pe";
        var central = new StubCentral
        {
            Result = new CentralLoginResult(first, "First", Role.Encargado, false, true, "Bodega 1")
        };
        await using (var ctx = NewCtx())
            await NewService(ctx, central).LoginAsync(new LoginRequest(first, "secret123"), CancellationToken.None);

        // A different account validates fine on the central but must be rejected by the binding.
        var second = $"second-{Guid.NewGuid():N}@bodega.pe";
        central.Result = new CentralLoginResult(second, "Second", Role.Encargado, false, true, "Bodega 2");

        await using var ctx2 = NewCtx();
        var svc = NewService(ctx2, central);
        var ex = await Assert.ThrowsAsync<AuthException>(() =>
            svc.LoginAsync(new LoginRequest(second, "secret123"), CancellationToken.None));
        Assert.Contains("otra bodega", ex.Message);
    }

    [Fact]
    public async Task Login_AdministratorOnBodegaInstall_IsRejected()
    {
        await ResetInstallAsync();
        var email = $"admin-{Guid.NewGuid():N}@sistema.pe";
        var central = new StubCentral
        {
            Result = new CentralLoginResult(email, "Admin", Role.Administrator, false, true, null)
        };
        await using var ctx = NewCtx();
        var svc = NewService(ctx, central);
        await Assert.ThrowsAsync<AuthException>(() =>
            svc.LoginAsync(new LoginRequest(email, "secret123"), CancellationToken.None));
    }

    private sealed class StubCentral : ICentralAuthClient
    {
        public CentralLoginResult? Result { get; set; }
        public bool Offline { get; set; }
        public bool RejectAll { get; set; }

        public Task<CentralLoginResult?> ValidateAsync(string email, string password, CancellationToken ct)
        {
            if (RejectAll) throw new CentralRejectedException("Credenciales inválidas.");
            if (Offline) return Task.FromResult<CentralLoginResult?>(null);
            return Task.FromResult(Result);
        }

        public Task ChangePasswordAsync(string email, string current, string @new, CancellationToken ct) =>
            Task.CompletedTask;
    }
}
