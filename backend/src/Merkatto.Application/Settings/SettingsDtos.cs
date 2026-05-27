namespace Merkatto.Application.Settings;

public record PublicSettingsDto(string BusinessName);

public record UpdateBusinessNameRequest(string BusinessName);
