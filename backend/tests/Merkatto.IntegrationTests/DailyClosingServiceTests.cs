using Merkatto.Application.Common;
using Merkatto.Application.Operations;
using Merkatto.Domain.Operations;
using Merkatto.Infrastructure.Persistence;
using Merkatto.IntegrationTests.Infrastructure;

namespace Merkatto.IntegrationTests;

public abstract class DailyClosingServiceTestsBase
{
    protected abstract AppDbContext NewCtx();

    private static readonly DateOnly BaseDate = new(2022, 1, 1);
    private static int _dayCounter;

    private static DateOnly NextDate() =>
        BaseDate.AddDays(Interlocked.Increment(ref _dayCounter));

    [Fact]
    public async Task CreateAsync_SumsExpensesAndSetsNetFlow()
    {
        var businessDate = NextDate();

        await using (var ctx = NewCtx())
        {
            ctx.Expenses.AddRange(
                new Expense { Date = businessDate, Type = ExpenseType.Luz, Amount = 30m },
                new Expense { Date = businessDate, Type = ExpenseType.Agua, Amount = 20m });
            await ctx.SaveChangesAsync();
        }

        long id;
        await using (var ctx = NewCtx())
        {
            var svc = new DailyClosingService(ctx, new StubClock());
            id = await svc.CreateAsync(new CreateDailyClosingRequest(
                businessDate,
                CashAmount: 200m, YapeAmount: 100m, PlinAmount: 0m,
                PosAmount: 100m, PosCommissionRate: 0.035m,
                QuickPurchases: 0m, Notes: null), CancellationToken.None);
        }

        await using var verify = NewCtx();
        var detail = await new DailyClosingService(verify, new StubClock())
            .GetByIdAsync(id, CancellationToken.None);

        Assert.Equal(400m, detail.GrossIncome);
        Assert.Equal(50m, detail.TotalExpenses);
        Assert.Equal(3.50m, detail.PosCommissionAmount);
        Assert.Equal(346.50m, detail.NetFlow);
    }

    [Fact]
    public async Task CreateAsync_ThrowsConflict_WhenDateAlreadyClosed()
    {
        var businessDate = NextDate();

        await using (var ctx = NewCtx())
        {
            var svc = new DailyClosingService(ctx, new StubClock());
            await svc.CreateAsync(new CreateDailyClosingRequest(
                businessDate, 100m, 0m, 0m, 0m, 0m, 0m, null), CancellationToken.None);
        }

        await using var ctx2 = NewCtx();
        var svc2 = new DailyClosingService(ctx2, new StubClock());
        await Assert.ThrowsAsync<ConflictException>(() =>
            svc2.CreateAsync(new CreateDailyClosingRequest(
                businessDate, 200m, 0m, 0m, 0m, 0m, 0m, null), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ThrowsBusinessRule_WhenClosingIsOlderThan7Days()
    {
        var businessDate = new DateOnly(2024, 1, 1);
        var clockAtClose = new StubClock { UtcNow = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero) };

        long id;
        await using (var ctx = NewCtx())
        {
            var svc = new DailyClosingService(ctx, clockAtClose);
            id = await svc.CreateAsync(new CreateDailyClosingRequest(
                businessDate, 100m, 0m, 0m, 0m, 0m, 0m, null), CancellationToken.None);
        }

        var clockNow = new StubClock { UtcNow = new DateTimeOffset(2024, 1, 10, 12, 0, 0, TimeSpan.Zero) };
        await using var ctx2 = NewCtx();
        var svc2 = new DailyClosingService(ctx2, clockNow);
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            svc2.UpdateAsync(id, new CreateDailyClosingRequest(
                businessDate, 200m, 0m, 0m, 0m, 0m, 0m, null), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_RefreshesExpensesAndRecalculatesNetFlow()
    {
        var businessDate = NextDate();
        var clock = new StubClock { UtcNow = new DateTimeOffset(businessDate.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero) };

        long id;
        await using (var ctx = NewCtx())
        {
            var svc = new DailyClosingService(ctx, clock);
            id = await svc.CreateAsync(new CreateDailyClosingRequest(
                businessDate, 100m, 0m, 0m, 0m, 0m, 0m, null), CancellationToken.None);
        }

        await using (var ctx = NewCtx())
        {
            ctx.Expenses.Add(new Expense { Date = businessDate, Type = ExpenseType.Movilidad, Amount = 40m });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx())
        {
            var svc = new DailyClosingService(ctx, clock);
            await svc.UpdateAsync(id, new CreateDailyClosingRequest(
                businessDate, 100m, 0m, 0m, 0m, 0m, 0m, null), CancellationToken.None);
        }

        await using var verify = NewCtx();
        var detail = await new DailyClosingService(verify, clock)
            .GetByIdAsync(id, CancellationToken.None);

        Assert.Equal(40m, detail.TotalExpenses);
        Assert.Equal(60m, detail.NetFlow);
    }
}

[Collection("Postgres")]
public sealed class DailyClosingServiceTests_Postgres(PostgresFixture f) : DailyClosingServiceTestsBase
{
    protected override AppDbContext NewCtx() => f.CreateContext();
}

[Collection("Sqlite")]
public sealed class DailyClosingServiceTests_Sqlite(SqliteFixture f) : DailyClosingServiceTestsBase
{
    protected override AppDbContext NewCtx() => f.CreateContext();
}
