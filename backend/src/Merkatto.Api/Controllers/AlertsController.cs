using Merkatto.Application.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/alerts")]
[Authorize]
public sealed class AlertsController(AlertService alerts) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AlertItem>> Get(CancellationToken ct) =>
        alerts.GetAlertsAsync(ct);
}
