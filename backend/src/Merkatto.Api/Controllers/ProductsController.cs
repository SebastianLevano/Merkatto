using Merkatto.Application.Catalog;
using Merkatto.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize] // any authenticated user can read
public sealed class ProductsController(ProductService products) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<ProductListItem>> List(
        [FromQuery] PagedQuery query, [FromQuery] bool? activeOnly, [FromQuery] long? categoryId, CancellationToken ct) =>
        products.GetAsync(query, activeOnly, categoryId, ct);

    [HttpGet("{id:long}")]
    public Task<ProductDetail> Get(long id, CancellationToken ct) => products.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Policy = "Encargado")]
    public async Task<ActionResult> Create(CreateProductRequest request, CancellationToken ct)
    {
        var id = await products.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "Encargado")]
    public async Task<IActionResult> Update(long id, UpdateProductRequest request, CancellationToken ct)
    {
        await products.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/activate")]
    [Authorize(Policy = "Encargado")]
    public async Task<IActionResult> Activate(long id, CancellationToken ct)
    {
        await products.SetActiveAsync(id, true, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/deactivate")]
    [Authorize(Policy = "Encargado")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        await products.SetActiveAsync(id, false, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = "Administrator")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await products.DeleteAsync(id, ct);
        return NoContent();
    }
}
