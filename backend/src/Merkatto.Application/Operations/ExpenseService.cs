using FluentValidation;
using Merkatto.Application.Common;
using Merkatto.Domain.Operations;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Operations;

public sealed class ExpenseService(IAppDbContext db)
{
    public async Task<PagedResult<ExpenseItem>> GetAsync(
        DateOnly? from, DateOnly? to, ExpenseType? type, PagedQuery query, CancellationToken ct)
    {
        var q = Filter(from, to, type);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .Select(e => new ExpenseItem(e.Id, e.Date, e.Type, e.Amount, e.Description))
            .ToListAsync(ct);

        return new PagedResult<ExpenseItem>(items, total, query.Page, query.PageSize);
    }

    public async Task<ExpenseSummary> GetSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var q = Filter(from, to, null);
        var byType = await q
            .GroupBy(e => e.Type)
            .Select(g => new ExpenseByType(g.Key, g.Sum(e => e.Amount)))
            .ToListAsync(ct);

        return new ExpenseSummary(byType.Sum(b => b.Total), byType.OrderByDescending(b => b.Total).ToList());
    }

    public async Task<long> CreateAsync(CreateExpenseRequest req, CancellationToken ct)
    {
        var expense = new Expense
        {
            Date = req.Date,
            Type = req.Type,
            Amount = req.Amount,
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim()
        };
        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);
        return expense.Id;
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException("Gasto no encontrado.");
        db.Expenses.Remove(expense); // soft-deleted by the auditing interceptor
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<Expense> Filter(DateOnly? from, DateOnly? to, ExpenseType? type)
    {
        var q = db.Expenses.AsQueryable();
        if (from is { } f) q = q.Where(e => e.Date >= f);
        if (to is { } t) q = q.Where(e => e.Date <= t);
        if (type is { } ty) q = q.Where(e => e.Type == ty);
        return q;
    }
}

public sealed class CreateExpenseValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(300);
    }
}
