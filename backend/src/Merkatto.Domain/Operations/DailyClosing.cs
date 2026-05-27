using Merkatto.Domain.Common;

namespace Merkatto.Domain.Operations;

/// <summary>
/// End-of-day cash-up. Income is captured by payment method; the POS commission is deducted
/// from net flow. Estimated profit is referential (derived from estimated margins).
/// </summary>
public class DailyClosing : BaseEntity
{
    public DateOnly BusinessDate { get; set; }

    public decimal CashAmount { get; set; }
    public decimal YapeAmount { get; set; }
    public decimal PlinAmount { get; set; }
    public decimal PosAmount { get; set; }

    /// <summary>POS commission rate (e.g. 0.035 = 3.5%).</summary>
    public decimal PosCommissionRate { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal QuickPurchases { get; set; }
    public decimal EstimatedProfit { get; set; }

    public string? Notes { get; set; }
    public DateTimeOffset ClosedAt { get; set; }

    public decimal PosCommissionAmount => Math.Round(PosAmount * PosCommissionRate, 2);
    public decimal GrossIncome => CashAmount + YapeAmount + PlinAmount + PosAmount;
    public decimal NetFlow => GrossIncome - TotalExpenses - QuickPurchases - PosCommissionAmount;
}
