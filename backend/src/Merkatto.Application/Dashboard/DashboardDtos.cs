namespace Merkatto.Application.Dashboard;

public record DashboardSummary(
    DashboardLastClosing? LastClosing,
    decimal TodayExpenses,
    int LowStockCount,
    decimal TotalCreditBalance,
    int ActiveCreditCustomers,
    IReadOnlyList<DashboardClosingRow> RecentClosings,
    int AlertCount,
    IReadOnlyList<DashboardTopProduct> TopProducts
);

public record DashboardLastClosing(
    DateOnly Date,
    decimal GrossIncome,
    decimal NetFlow,
    decimal EstimatedProfit
);

public record DashboardClosingRow(DateOnly Date, decimal GrossIncome, decimal NetFlow);

public record DashboardTopProduct(
    long ProductId,
    string Name,
    string? Category,
    decimal SalePrice,
    decimal Margin,
    decimal MarginRate
);
