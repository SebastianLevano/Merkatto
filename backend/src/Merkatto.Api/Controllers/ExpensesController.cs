using Merkatto.Application.Common;
using Merkatto.Application.Operations;
using Merkatto.Domain.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/expenses")]
[Authorize]
public sealed class ExpensesController(ExpenseService expenses) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<ExpenseItem>> List(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] ExpenseType? type,
        [FromQuery] PagedQuery query, CancellationToken ct) =>
        expenses.GetAsync(from, to, type, query, ct);

    [HttpGet("summary")]
    public Task<ExpenseSummary> Summary(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) =>
        expenses.GetSummaryAsync(from, to, ct);

    [HttpGet("{id:long}")]
    public Task<ExpenseItem> Get(long id, CancellationToken ct) => expenses.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Policy = "Collaborator")]
    public async Task<ActionResult> Create(CreateExpenseRequest request, CancellationToken ct)
    {
        var id = await expenses.CreateAsync(request, ct);
        return Created($"/api/v1/expenses/{id}", new { id });
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "Administrator")]
    public async Task<IActionResult> Update(long id, CreateExpenseRequest request, CancellationToken ct)
    {
        await expenses.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = "Administrator")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await expenses.DeleteAsync(id, ct);
        return NoContent();
    }
}
