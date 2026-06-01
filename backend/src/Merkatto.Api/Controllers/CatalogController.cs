using Merkatto.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
[Authorize]
public sealed class CategoriesController(CategoryBrandService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<LookupItem>> List(CancellationToken ct) => service.GetCategoriesAsync(ct);

    [HttpPost]
    [Authorize(Policy = "Encargado")]
    public async Task<ActionResult> Create(NameRequest request, CancellationToken ct)
    {
        var id = await service.CreateCategoryAsync(request, ct);
        return Created($"/api/v1/categories/{id}", new { id });
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "Encargado")]
    public async Task<IActionResult> Update(long id, NameRequest request, CancellationToken ct)
    {
        await service.UpdateCategoryAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = "Administrator")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteCategoryAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/brands")]
[Authorize]
public sealed class BrandsController(CategoryBrandService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<LookupItem>> List(CancellationToken ct) => service.GetBrandsAsync(ct);

    [HttpPost]
    [Authorize(Policy = "Administrator")]
    public async Task<ActionResult> Create(NameRequest request, CancellationToken ct)
    {
        var id = await service.CreateBrandAsync(request, ct);
        return Created($"/api/v1/brands/{id}", new { id });
    }
}
