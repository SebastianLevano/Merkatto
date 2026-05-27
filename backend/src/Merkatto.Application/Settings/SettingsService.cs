using Merkatto.Application.Common;
using Merkatto.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Settings;

public sealed class SettingsService(IAppDbContext db)
{
    public async Task<PublicSettingsDto> GetPublicAsync(CancellationToken ct)
    {
        var businessName = await db.AppSettings
            .Where(s => s.Key == AppSettingKeys.BusinessName)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct) ?? "Mi Bodega";

        return new PublicSettingsDto(businessName);
    }

    public async Task UpdateBusinessNameAsync(UpdateBusinessNameRequest req, CancellationToken ct)
    {
        var name = req.BusinessName.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 120)
            throw new BusinessRuleException("El nombre del negocio debe tener entre 1 y 120 caracteres.");

        var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == AppSettingKeys.BusinessName, ct);
        if (setting is null)
        {
            db.AppSettings.Add(new AppSetting { Key = AppSettingKeys.BusinessName, Value = name });
        }
        else
        {
            setting.Value = name;
        }

        await db.SaveChangesAsync(ct);
    }
}
