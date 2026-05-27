namespace Merkatto.Application.Alerts;

public enum AlertType
{
    StockLow,
    StockOut,
    NoClosure,
    HighPendingCredit
}

public enum AlertSeverity { Info, Warning, Critical }

public record AlertItem(
    AlertType Type,
    AlertSeverity Severity,
    string Message,
    string? Reference
);
