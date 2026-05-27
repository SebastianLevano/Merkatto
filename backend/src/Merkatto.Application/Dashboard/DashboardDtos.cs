namespace Merkatto.Application.Dashboard;

public record DashboardSummary(
    DashboardLastClosing? LastClosing,
    decimal TodayExpenses,
    int LowStockCount,
    decimal TotalCreditBalance,
    int ActiveCreditCustomers,
    IReadOnlyList<DashboardClosingRow> RecentClosings
);

public record DashboardLastClosing(
    DateOnly Date,
    decimal GrossIncome,
    decimal NetFlow,
    decimal EstimatedProfit
);

public record DashboardClosingRow(DateOnly Date, decimal GrossIncome, decimal NetFlow);
