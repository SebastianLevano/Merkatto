using ClosedXML.Excel;
using Merkatto.Application.Common;
using Merkatto.Application.Reports;
using Merkatto.Domain.Operations;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Merkatto.Infrastructure.Reports;

public sealed class ReportService(IAppDbContext db) : IReportService
{
    // ── PDF: resumen mensual de cierres ─────────────────────────────────────

    public async Task<byte[]> ClosingsPdfAsync(int year, int month, CancellationToken ct)
    {
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var monthName = new System.Globalization.CultureInfo("es-PE").DateTimeFormat.GetMonthName(month);

        var closings = await db.DailyClosings
            .Where(c => c.BusinessDate >= from && c.BusinessDate <= to)
            .OrderBy(c => c.BusinessDate)
            .ToListAsync(ct);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text($"Merkatto — Cierres de {monthName} {year}")
                        .Bold().FontSize(14);
                    col.Item().Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(4).LineHorizontal(1);
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    if (closings.Count == 0)
                    {
                        col.Item().Text("No hay cierres registrados para este mes.").Italic();
                        return;
                    }

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(70);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        // Header
                        static IContainer HeaderCell(IContainer c) =>
                            c.Background(Colors.Grey.Lighten3).Padding(4);

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("Fecha").Bold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Ingresos").Bold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Gastos").Bold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Flujo neto").Bold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("Utilidad est.").Bold();
                        });

                        // Rows
                        foreach (var c in closings)
                        {
                            static IContainer BodyCell(IContainer cell) =>
                                cell.BorderBottom(0.5f).Padding(4);

                            table.Cell().Element(BodyCell).Text(c.BusinessDate.ToString("dd/MM/yyyy"));
                            table.Cell().Element(BodyCell).AlignRight().Text($"S/ {c.GrossIncome:N2}");
                            table.Cell().Element(BodyCell).AlignRight().Text($"S/ {c.TotalExpenses:N2}");
                            table.Cell().Element(BodyCell).AlignRight()
                                .Text($"S/ {c.NetFlow:N2}")
                                .FontColor(c.NetFlow >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                            table.Cell().Element(BodyCell).AlignRight().Text($"S/ {c.EstimatedProfit:N2}");
                        }
                    });

                    // Totals
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(300).Column(totals =>
                        {
                            totals.Item().LineHorizontal(1);
                            SummaryRow(totals, "Total ingresos:", closings.Sum(c => c.GrossIncome));
                            SummaryRow(totals, "Total gastos:", closings.Sum(c => c.TotalExpenses));
                            SummaryRow(totals, "Flujo neto total:", closings.Sum(c => c.NetFlow));
                        });
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span(" de ");
                    t.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static void SummaryRow(ColumnDescriptor col, string label, decimal value) =>
        col.Item().Row(r =>
        {
            r.RelativeItem().Text(label).Bold();
            r.ConstantItem(100).AlignRight().Text($"S/ {value:N2}");
        });

    // ── Excel: gastos del mes ───────────────────────────────────────────────

    public async Task<byte[]> ExpensesExcelAsync(int year, int month, CancellationToken ct)
    {
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var monthName = new System.Globalization.CultureInfo("es-PE").DateTimeFormat.GetMonthName(month);

        var expenses = await db.Expenses
            .Where(e => e.Date >= from && e.Date <= to)
            .OrderBy(e => e.Date).ThenBy(e => e.Id)
            .ToListAsync(ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Gastos {monthName} {year}");

        // Title
        ws.Cell(1, 1).Value = $"Merkatto — Gastos de {monthName} {year}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, 4).Merge();

        // Headers
        var headers = new[] { "Fecha", "Tipo", "Descripción", "Monto (S/)" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        // Data
        var row = 4;
        foreach (var e in expenses)
        {
            ws.Cell(row, 1).Value = e.Date.ToString("dd/MM/yyyy");
            ws.Cell(row, 2).Value = ExpenseTypeLabel(e.Type);
            ws.Cell(row, 3).Value = e.Description ?? "";
            ws.Cell(row, 4).Value = (double)e.Amount;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        // Total row
        if (expenses.Count > 0)
        {
            ws.Cell(row, 3).Value = "TOTAL";
            ws.Cell(row, 3).Style.Font.Bold = true;
            ws.Cell(row, 4).FormulaA1 = $"=SUM(D4:D{row - 1})";
            ws.Cell(row, 4).Style.Font.Bold = true;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 4).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string ExpenseTypeLabel(ExpenseType type) => type switch
    {
        ExpenseType.Luz => "Luz",
        ExpenseType.Agua => "Agua",
        ExpenseType.Movilidad => "Movilidad",
        ExpenseType.Reposicion => "Reposición",
        ExpenseType.Mantenimiento => "Mantenimiento",
        ExpenseType.CompraRapida => "Compra rápida",
        ExpenseType.Otros => "Otros",
        _ => type.ToString()
    };
}
