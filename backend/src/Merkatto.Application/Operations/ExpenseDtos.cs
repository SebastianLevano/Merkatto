using Merkatto.Domain.Operations;

namespace Merkatto.Application.Operations;

public record ExpenseItem(long Id, DateOnly Date, ExpenseType Type, decimal Amount, string? Description);

public record CreateExpenseRequest(DateOnly Date, ExpenseType Type, decimal Amount, string? Description);

public record ExpenseByType(ExpenseType Type, decimal Total);

public record ExpenseSummary(decimal Total, IReadOnlyList<ExpenseByType> ByType);
