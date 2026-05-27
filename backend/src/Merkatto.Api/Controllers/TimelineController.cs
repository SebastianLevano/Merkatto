using Merkatto.Application.Audit;
using Merkatto.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/timeline")]
[Authorize(Policy = "Administrator")]
public sealed class TimelineController(TimelineService timeline) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<TimelineEntry>> List([FromQuery] PagedQuery query, CancellationToken ct) =>
        timeline.GetAsync(query, ct);
}
