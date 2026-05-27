namespace Merkatto.Application.Operations;

public record CreateDailyClosingRequest(
    DateOnly BusinessDate,
    decimal CashAmount,
    decimal YapeAmount,
    decimal PlinAmount,
    decimal PosAmount,
    decimal PosCommissionRate,
    decimal QuickPurchases,
    string? Notes);

public record DailyClosingListItem(
    long Id,
    DateOnly BusinessDate,
    decimal GrossIncome,
    decimal NetFlow,
    decimal EstimatedProfit);

public record DailyClosingDetail(
    long Id,
    DateOnly BusinessDate,
    decimal CashAmount,
    decimal YapeAmount,
    decimal PlinAmount,
    decimal PosAmount,
    decimal PosCommissionRate,
    decimal PosCommissionAmount,
    decimal TotalExpenses,
    decimal QuickPurchases,
    decimal GrossIncome,
    decimal NetFlow,
    decimal EstimatedProfit,
    string? Notes,
    DateTimeOffset ClosedAt);

/// <summary>Pre-fill data for the closing form: the day's already-registered expenses.</summary>
public record DailyClosingPreview(DateOnly BusinessDate, decimal TotalExpenses, bool AlreadyClosed);
