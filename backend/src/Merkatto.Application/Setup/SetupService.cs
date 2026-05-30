using Merkatto.Application.Common;
using Merkatto.Domain.Auth;
using Merkatto.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Setup;

public sealed class SetupService(
    IAppDbContext db,
    IPasswordHasher hasher,
    IDateTimeProvider clock)
{
    public async Task<bool> NeedsSetupAsync(CancellationToken ct = default) =>
        !await db.Users.AnyAsync(ct);

    public async Task InitializeAsync(InitializeRequest req, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
            throw new BusinessRuleException("El sistema ya fue inicializado. Iniciá sesión normalmente.");

        db.Users.Add(new User
        {
            Email = req.AdminEmail.Trim().ToLowerInvariant(),
            FullName = req.AdminName.Trim(),
            PasswordHash = hasher.Hash(req.AdminPassword),
            Role = Role.Administrator,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = clock.UtcNow,
            CreatedBy = "setup"
        });

        var setting = await db.AppSettings.FindAsync([AppSettingKeys.BusinessName], ct);
        if (setting != null && !string.IsNullOrWhiteSpace(req.BusinessName))
            setting.Value = req.BusinessName.Trim();

        await db.SaveChangesAsync(ct);
    }
}
