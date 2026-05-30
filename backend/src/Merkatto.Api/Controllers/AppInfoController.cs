using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/app")]
public sealed class AppInfoController : ControllerBase
{
    [HttpGet("info")]
    [AllowAnonymous]
    public IActionResult Info([FromServices] AppUpdateState? state)
    {
        var version = typeof(AppInfoController).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        return Ok(new
        {
            version,
            updateAvailable = state?.UpdateAvailable ?? false,
            updateVersion   = state?.UpdateVersion
        });
    }
}

/// <summary>
/// Singleton shared between the update checker and the AppInfoController.
/// Registered only by the Desktop host; cloud returns updateAvailable=false.
/// </summary>
public sealed class AppUpdateState
{
    public bool UpdateAvailable { get; set; }
    public string? UpdateVersion { get; set; }
}
