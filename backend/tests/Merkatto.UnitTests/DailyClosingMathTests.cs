using Merkatto.Domain.Operations;
using Xunit;

namespace Merkatto.UnitTests;

public class DailyClosingMathTests
{
    private static DailyClosing Sample() => new()
    {
        BusinessDate = new DateOnly(2026, 5, 27),
        CashAmount = 300m,
        YapeAmount = 150m,
        PlinAmount = 50m,
        PosAmount = 200m,
        PosCommissionRate = 0.035m,
        TotalExpenses = 80m,
        QuickPurchases = 40m
    };

    [Fact]
    public void GrossIncome_SumsAllPaymentMethods()
    {
        Assert.Equal(700m, Sample().GrossIncome);
    }

    [Fact]
    public void PosCommission_IsRateAppliedToPosAmount()
    {
        Assert.Equal(7.00m, Sample().PosCommissionAmount); // 200 * 3.5%
    }

    [Fact]
    public void NetFlow_DeductsExpensesQuickPurchasesAndCommission()
    {
        // 700 - 80 - 40 - 7 = 573
        Assert.Equal(573m, Sample().NetFlow);
    }
}
