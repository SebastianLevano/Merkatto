namespace Merkatto.Application.Reports;

public interface IReportService
{
    Task<byte[]> ClosingsPdfAsync(int year, int month, CancellationToken ct);
    Task<byte[]> ExpensesExcelAsync(int year, int month, CancellationToken ct);
}
