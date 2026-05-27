using Merkatto.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/settings")]
public sealed class SettingsController(SettingsService settings) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public Task<PublicSettingsDto> Get(CancellationToken ct) =>
        settings.GetPublicAsync(ct);

    [HttpPut("business-name")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> UpdateBusinessName(UpdateBusinessNameRequest request, CancellationToken ct)
    {
        await settings.UpdateBusinessNameAsync(request, ct);
        return NoContent();
    }
}
