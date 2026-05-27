using Merkatto.Application.Common;
using Merkatto.Application.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/inventory")]
[Authorize]
public sealed class InventoryController(InventoryService inventory) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<InventoryRow>> List(
        [FromQuery] PagedQuery query, [FromQuery] bool lowStockOnly, CancellationToken ct) =>
        inventory.GetAsync(query, lowStockOnly, ct);

    [HttpGet("movements")]
    public Task<PagedResult<MovementRow>> Movements(
        [FromQuery] long? productId, [FromQuery] PagedQuery query, CancellationToken ct) =>
        inventory.GetMovementsAsync(productId, query, ct);

    [HttpPost("transfers")]
    [Authorize(Policy = "Collaborator")]
    public async Task<IActionResult> Transfer(TransferRequest request, CancellationToken ct)
    {
        await inventory.TransferAsync(request, ct);
        return NoContent();
    }

    [HttpPost("adjustments")]
    [Authorize(Policy = "Collaborator")]
    public async Task<IActionResult> Adjust(AdjustmentRequest request, CancellationToken ct)
    {
        await inventory.AdjustAsync(request, ct);
        return NoContent();
    }

    [HttpPost("batch-count")]
    [Authorize(Policy = "Collaborator")]
    public async Task<IActionResult> BatchCount(BatchCountRequest request, CancellationToken ct)
    {
        await inventory.BatchCountAsync(request, ct);
        return NoContent();
    }
}
