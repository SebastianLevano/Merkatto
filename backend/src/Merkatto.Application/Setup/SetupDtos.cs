namespace Merkatto.Application.Setup;

public record SetupStatusDto(bool NeedsSetup, string? DefaultBusinessName, string? DefaultAdminEmail);

public record InitializeRequest(
    string BusinessName,
    string AdminName,
    string AdminEmail,
    string AdminPassword);
