using Merkatto.Application.Nrus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/nrus")]
[Authorize]
public sealed class NrusController(NrusService nrus) : ControllerBase
{
    [HttpGet("estimate")]
    public Task<NrusMonthEstimate> Estimate([FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
    {
        var today = DateTime.UtcNow;
        return nrus.GetEstimateAsync(year ?? today.Year, month ?? today.Month, ct);
    }

    [HttpGet("history")]
    public Task<IReadOnlyList<NrusMonthEstimate>> History([FromQuery] int months = 6, CancellationToken ct = default) =>
        nrus.GetHistoryAsync(Math.Clamp(months, 1, 24), ct);
}
