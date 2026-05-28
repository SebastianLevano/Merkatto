using Merkatto.Application.Common;
using Merkatto.Application.Operations;
using Merkatto.Domain.Operations;
using Merkatto.IntegrationTests.Infrastructure;

namespace Merkatto.IntegrationTests;

[Collection("Database")]
public class DailyClosingServiceTests(DatabaseFixture db)
{
    private static readonly DateOnly BaseDate = new(2022, 1, 1);
    private static int _dayCounter;

    // Each test gets a unique date to avoid the unique-constraint on BusinessDate.
    private static DateOnly NextDate() =>
        BaseDate.AddDays(Interlocked.Increment(ref _dayCounter));

    [Fact]
    public async Task CreateAsync_SumsExpensesAndSetsNetFlow()
    {
        var businessDate = NextDate();

        // Seed two expenses for that date.
        await using (var ctx = db.CreateContext())
        {
            ctx.Expenses.AddRange(
                new Expense { Date = businessDate, Type = ExpenseType.Luz, Amount = 30m },
                new Expense { Date = businessDate, Type = ExpenseType.Agua, Amount = 20m });
            await ctx.SaveChangesAsync();
        }

        long id;
        await using (var ctx = db.CreateContext())
        {
            var svc = new DailyClosingService(ctx, new StubClock());
            id = await svc.CreateAsync(new CreateDailyClosingRequest(
                businessDate,
                CashAmount: 200m, YapeAmount: 100m, PlinAmount: 0m,
                PosAmount: 100m, PosCommissionRate: 0.035m,
                QuickPurchases: 0m, Notes: null), CancellationToken.None);
        }

        await using var verify = db.CreateContext();
        var detail = await new DailyClosingService(verify, new StubClock())
            .GetByIdAsync(id, CancellationToken.None);

        Assert.Equal(400m, detail.GrossIncome);          // 200+100+0+100
        Assert.Equal(50m, detail.TotalExpenses);         // 30+20 from DB
        Assert.Equal(3.50m, detail.PosCommissionAmount); // 100 × 0.035
        Assert.Equal(346.50m, detail.NetFlow);           // 400 - 50 - 0 - 3.50
    }

    [Fact]
    public async Task CreateAsync_ThrowsConflict_WhenDateAlreadyClosed()
    {
        var businessDate = NextDate();

        await using (var ctx = db.CreateContext())
        {
            var svc = new DailyClosingService(ctx, new StubClock());
            await svc.CreateAsync(new CreateDailyClosingRequest(
                businessDate, 100m, 0m, 0m, 0m, 0m, 0m, null), CancellationToken.None);
        }

        await using var ctx2 = db.CreateContext();
        var svc2 = new DailyClosingService(ctx2, new StubClock());
        await Assert.ThrowsAsync<ConflictException>(() =>
            svc2.CreateAsync(new CreateDailyClosingRequest(
                businessDate, 200m, 0m, 0m, 0m, 0m, 0m, null), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ThrowsBusinessRule_WhenClosingIsOlderThan7Days()
    {
        var businessDate = new DateOnly(2024, 1, 1);

        long id;
        var clockAtClose = new StubClock { UtcNow = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero) };
        await using (var ctx = db.CreateContext())
        {
            var svc = new DailyClosingService(ctx, clockAtClose);
            id = await svc.CreateAsync(new CreateDailyClosingRequest(
                businessDate, 100m, 0m, 0m, 0m, 0m, 0m, null), CancellationToken.None);
        }

        // 9 days later — beyond the 7-day edit window.
        var clockNow = new StubClock { UtcNow = new DateTimeOffset(2024, 1, 10, 12, 0, 0, TimeSpan.Zero) };
        await using var ctx2 = db.CreateContext();
        var svc2 = new DailyClosingService(ctx2, clockNow);
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            svc2.UpdateAsync(id, new CreateDailyClosingRequest(
                businessDate, 200m, 0m, 0m, 0m, 0m, 0m, null), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_RefreshesExpensesAndRecalculatesNetFlow()
    {
        var businessDate = NextDate();
        // Clock must be within 7 days of businessDate so the edit-window check passes.
        var clock = new StubClock { UtcNow = new DateTimeOffset(businessDate.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero) };

        long id;
        await using (var ctx = db.CreateContext())
        {
            var svc = new DailyClosingService(ctx, clock);
            id = await svc.CreateAsync(new CreateDailyClosingRequest(
                businessDate, 100m, 0m, 0m, 0m, 0m, 0m, null), CancellationToken.None);
        }

        // Add an expense after the closing was created.
        await using (var ctx = db.CreateContext())
        {
            ctx.Expenses.Add(new Expense { Date = businessDate, Type = ExpenseType.Movilidad, Amount = 40m });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = db.CreateContext())
        {
            var svc = new DailyClosingService(ctx, clock);
            await svc.UpdateAsync(id, new CreateDailyClosingRequest(
                businessDate, 100m, 0m, 0m, 0m, 0m, 0m, null), CancellationToken.None);
        }

        await using var verify = db.CreateContext();
        var detail = await new DailyClosingService(verify, clock)
            .GetByIdAsync(id, CancellationToken.None);

        Assert.Equal(40m, detail.TotalExpenses);    // refreshed from DB
        Assert.Equal(60m, detail.NetFlow);          // 100 - 40
    }
}
