using Merkatto.Application.Common;
using Merkatto.Application.Purchasing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/purchases")]
[Authorize]
public sealed class PurchasesController(PurchaseService purchases) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<PurchaseListItem>> List([FromQuery] PagedQuery query, CancellationToken ct) =>
        purchases.GetAsync(query, ct);

    [HttpGet("{id:long}")]
    public Task<PurchaseDetail> Get(long id, CancellationToken ct) => purchases.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Policy = "Administrator")]
    public async Task<ActionResult> Create(CreatePurchaseRequest request, CancellationToken ct)
    {
        var id = await purchases.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }
}

[ApiController]
[Route("api/v1/suppliers")]
[Authorize]
public sealed class SuppliersController(SupplierService suppliers) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<SupplierItem>> List(CancellationToken ct) => suppliers.GetAsync(ct);

    [HttpPost]
    [Authorize(Policy = "Administrator")]
    public async Task<ActionResult> Create(SaveSupplierRequest request, CancellationToken ct)
    {
        var id = await suppliers.CreateAsync(request, ct);
        return Created($"/api/v1/suppliers/{id}", new { id });
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "Administrator")]
    public async Task<IActionResult> Update(long id, SaveSupplierRequest request, CancellationToken ct)
    {
        await suppliers.UpdateAsync(id, request, ct);
        return NoContent();
    }
}
