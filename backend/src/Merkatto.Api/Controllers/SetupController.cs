using Merkatto.Application.Setup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/setup")]
public sealed class SetupController(SetupService setup) : ControllerBase
{
    /// <summary>
    /// Returns whether first-run setup is still needed.
    /// Optionally includes pre-fill data from client.json if it was injected at startup.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<SetupStatusDto> Status(
        [FromServices] SetupPrefill? prefill,
        CancellationToken ct)
    {
        var needs = await setup.NeedsSetupAsync(ct);
        return new SetupStatusDto(needs, prefill?.BusinessName, prefill?.AdminEmail);
    }

    [HttpPost("initialize")]
    [AllowAnonymous]
    public async Task<IActionResult> Initialize(InitializeRequest req, CancellationToken ct)
    {
        await setup.InitializeAsync(req, ct);
        return NoContent();
    }

    /// <summary>Triggers an on-demand backup; desktop-only (BackupTrigger is not registered in cloud).</summary>
    [HttpPost("backup")]
    [Authorize(Policy = "Administrator")]
    public IActionResult Backup([FromServices] BackupTrigger? trigger)
    {
        if (trigger is null)
            return StatusCode(StatusCodes.Status501NotImplemented, new { detail = "Backups manuales solo disponibles en modo escritorio." });
        var path = trigger.Invoke();
        return Ok(new { path });
    }
}

/// <summary>
/// Optional pre-fill data injected by the Desktop host from client.json.
/// Not registered in cloud mode.
/// </summary>
public sealed class SetupPrefill(string? businessName, string? adminEmail)
{
    public string? BusinessName { get; } = businessName;
    public string? AdminEmail   { get; } = adminEmail;
}

/// <summary>
/// Triggered by the Desktop host to perform an on-demand backup.
/// Not registered in cloud mode.
/// </summary>
public sealed class BackupTrigger(Func<string> createBackup)
{
    public string Invoke() => createBackup();
}
