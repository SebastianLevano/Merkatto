using Merkatto.Application.Auth;
using Merkatto.Application.Common;
using Merkatto.Domain.Auth;
using Merkatto.Domain.Catalog;
using Merkatto.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Merkatto.Infrastructure.Persistence;

/// <summary>
/// Applies migrations and seeds the initial administrator on first run of an installation.
/// Admin credentials come from configuration/environment (per-client .env).
/// </summary>
public sealed class DbInitializer(
    AppDbContext db,
    IPasswordHasher hasher,
    IDateTimeProvider clock,
    SeedSettings seed,
    ILogger<DbInitializer> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        await SeedBusinessNameAsync(ct);
        await SeedDefaultCategoriesAsync(ct);

        if (await db.Users.AnyAsync(ct))
            return;

        if (string.IsNullOrWhiteSpace(seed.AdminEmail) || string.IsNullOrWhiteSpace(seed.AdminPassword))
        {
            logger.LogWarning("No admin seed configured (Seed:AdminEmail/AdminPassword). Skipping admin creation.");
            return;
        }

        db.Users.Add(new User
        {
            Email = seed.AdminEmail.ToLower(),
            FullName = seed.AdminName,
            PasswordHash = hasher.Hash(seed.AdminPassword),
            Role = Role.Administrator,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = clock.UtcNow,
            CreatedBy = "system"
        });
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded administrator {Email}.", seed.AdminEmail);
    }

    /// <summary>
    /// Seeds a reasonable starter set of categories so a fresh install isn't blocked from creating
    /// products. Skips if any category already exists — operators can add/edit from the UI.
    /// </summary>
    private async Task SeedDefaultCategoriesAsync(CancellationToken ct)
    {
        if (await db.Categories.AnyAsync(ct)) return;

        string[] defaults =
        [
            "Abarrotes", "Bebidas", "Lácteos", "Panadería", "Golosinas", "Limpieza",
            "Higiene personal", "Cigarrillos", "Embutidos", "Congelados", "Snacks", "Otros"
        ];

        foreach (var name in defaults)
            db.Categories.Add(new Category { Name = name });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} default categories.", defaults.Length);
    }

    private async Task SeedBusinessNameAsync(CancellationToken ct)
    {
        var exists = await db.AppSettings.AnyAsync(s => s.Key == AppSettingKeys.BusinessName, ct);
        if (!exists)
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = AppSettingKeys.BusinessName,
                Value = string.IsNullOrWhiteSpace(seed.BusinessName) ? "Mi Bodega" : seed.BusinessName
            });
            await db.SaveChangesAsync(ct);
        }
    }
}

public sealed class SeedSettings
{
    public const string SectionName = "Seed";
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminName { get; set; } = "Administrador";
    public string AdminPassword { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
}
