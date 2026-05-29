using Merkatto.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public sealed class ReportsController(IReportService reports) : ControllerBase
{
    [HttpGet("closings")]
    public async Task<IActionResult> Closings([FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
    {
        var today = DateTime.UtcNow;
        var pdf = await reports.ClosingsPdfAsync(year ?? today.Year, month ?? today.Month, ct);
        var filename = $"cierres_{year ?? today.Year}_{(month ?? today.Month):D2}.pdf";
        return File(pdf, "application/pdf", filename);
    }

    [HttpGet("expenses")]
    public async Task<IActionResult> Expenses([FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
    {
        var today = DateTime.UtcNow;
        var xlsx = await reports.ExpensesExcelAsync(year ?? today.Year, month ?? today.Month, ct);
        var filename = $"gastos_{year ?? today.Year}_{(month ?? today.Month):D2}.xlsx";
        return File(xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
    }

    [HttpGet("purchases")]
    public async Task<IActionResult> Purchases([FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
    {
        var today = DateTime.UtcNow;
        var xlsx = await reports.PurchasesExcelAsync(year ?? today.Year, month ?? today.Month, ct);
        var filename = $"compras_{year ?? today.Year}_{(month ?? today.Month):D2}.xlsx";
        return File(xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
    }
}
