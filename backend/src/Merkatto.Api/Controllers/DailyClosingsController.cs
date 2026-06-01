using Merkatto.Application.Common;
using Merkatto.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/daily-closings")]
[Authorize]
public sealed class DailyClosingsController(DailyClosingService closings) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<DailyClosingListItem>> List(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] PagedQuery query, CancellationToken ct) =>
        closings.GetAsync(from, to, query, ct);

    [HttpGet("preview")]
    public Task<DailyClosingPreview> Preview([FromQuery] DateOnly date, CancellationToken ct) =>
        closings.PreviewAsync(date, ct);

    [HttpGet("by-id/{id:long}")]
    public Task<DailyClosingDetail> GetById(long id, CancellationToken ct) =>
        closings.GetByIdAsync(id, ct);

    [HttpGet("{date}")]
    public Task<DailyClosingDetail> GetByDate(DateOnly date, CancellationToken ct) =>
        closings.GetByDateAsync(date, ct);

    [HttpPost]
    [Authorize(Policy = "Encargado")]
    public async Task<ActionResult> Create(CreateDailyClosingRequest request, CancellationToken ct)
    {
        var id = await closings.CreateAsync(request, ct);
        return Created($"/api/v1/daily-closings/{request.BusinessDate:yyyy-MM-dd}", new { id });
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "Administrator")]
    public async Task<IActionResult> Update(long id, CreateDailyClosingRequest request, CancellationToken ct)
    {
        await closings.UpdateAsync(id, request, ct);
        return NoContent();
    }
}
