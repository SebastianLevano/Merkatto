using Merkatto.Domain.Common;

namespace Merkatto.Domain.Operations;

public enum ExpenseType
{
    Luz = 1,
    Agua = 2,
    Movilidad = 3,
    Reposicion = 4,
    Mantenimiento = 5,
    CompraRapida = 6,
    Otros = 99
}

public class Expense : BaseEntity
{
    public DateOnly Date { get; set; }
    public ExpenseType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }

    /// <summary>Optional link to the day's closing.</summary>
    public long? DailyClosingId { get; set; }
    public DailyClosing? DailyClosing { get; set; }
}
