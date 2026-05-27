using Merkatto.Application.Credit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/credit")]
[Authorize]
public sealed class CreditController(CreditService credit) : ControllerBase
{
    [HttpGet("customers")]
    public Task<IReadOnlyList<CreditCustomerListItem>> Customers([FromQuery] string? search, CancellationToken ct) =>
        credit.GetCustomersAsync(search, ct);

    [HttpGet("customers/{id:long}")]
    public Task<CreditCustomerDetail> Customer(long id, CancellationToken ct) =>
        credit.GetCustomerAsync(id, ct);

    [HttpGet("customers/{id:long}/balance")]
    public async Task<ActionResult<object>> Balance(long id, CancellationToken ct) =>
        Ok(new { balance = await credit.GetBalanceAsync(id, ct) });

    [HttpPost("customers")]
    [Authorize(Policy = "Collaborator")]
    public async Task<ActionResult> CreateCustomer(SaveCustomerRequest request, CancellationToken ct)
    {
        var id = await credit.CreateCustomerAsync(request, ct);
        return Created($"/api/v1/credit/customers/{id}", new { id });
    }

    [HttpPost("sales")]
    [Authorize(Policy = "Collaborator")]
    public async Task<ActionResult> AddSale(CreateCreditSaleRequest request, CancellationToken ct)
    {
        var id = await credit.AddSaleAsync(request, ct);
        return Created($"/api/v1/credit/sales/{id}", new { id });
    }

    [HttpPost("payments")]
    [Authorize(Policy = "Collaborator")]
    public async Task<ActionResult> AddPayment(CreatePaymentRequest request, CancellationToken ct)
    {
        var id = await credit.AddPaymentAsync(request, ct);
        return Created($"/api/v1/credit/payments/{id}", new { id });
    }
}
