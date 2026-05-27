using Merkatto.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public sealed class DashboardController(DashboardService dashboard) : ControllerBase
{
    [HttpGet("summary")]
    public Task<DashboardSummary> Summary(CancellationToken ct) =>
        dashboard.GetSummaryAsync(ct);
}
