namespace Merkatto.Application.Nrus;

/// <summary>
/// Referential NRUS (Nuevo RUS) tax estimate for a single month.
/// DISCLAIMER: This is an informational estimate only. Consult a tax advisor.
/// </summary>
public record NrusMonthEstimate(
    int Year,
    int Month,
    decimal MonthlyIncome,
    decimal MonthlyPurchases,
    decimal MaxAmount,
    int? Category,       // 1 or 2, null if exceeds limit
    decimal? Quota,      // S/20 or S/50, null if exceeds
    bool ExceedsLimit
);

/// <summary>Brackets as of 2024-2025 Peru tax law.</summary>
public static class NrusBrackets
{
    public const decimal Cat1Limit = 5_000m;
    public const decimal Cat1Quota = 20m;
    public const decimal Cat2Limit = 8_000m;
    public const decimal Cat2Quota = 50m;

    public static (int? Category, decimal? Quota, bool ExceedsLimit) Classify(decimal maxAmount) =>
        maxAmount <= Cat1Limit ? (1, Cat1Quota, false) :
        maxAmount <= Cat2Limit ? (2, Cat2Quota, false) :
        (null, null, true);
}
