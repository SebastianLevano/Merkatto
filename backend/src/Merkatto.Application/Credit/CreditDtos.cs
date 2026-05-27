namespace Merkatto.Application.Credit;

public record CreditCustomerListItem(long Id, string Name, string? Phone, decimal Balance);

public record SaveCustomerRequest(string Name, string? Phone, string? Notes);

public record CreditSaleItemRequest(string Description, decimal Quantity, decimal LineTotal);

/// <summary>
/// Quick credit sale. Either provide free-text <see cref="Items"/> (total = sum of line totals)
/// or a single <see cref="Amount"/>.
/// </summary>
public record CreateCreditSaleRequest(
    long CustomerId,
    DateOnly Date,
    string? Notes,
    decimal? Amount,
    IReadOnlyList<CreditSaleItemRequest>? Items);

public record CreatePaymentRequest(long CustomerId, DateOnly Date, decimal Amount, string? Notes);

/// <summary>A unified timeline row: a charge (fiado) or a payment.</summary>
public record CreditHistoryEntry(
    string Kind,            // "sale" | "payment"
    long Id,
    DateOnly Date,
    decimal Amount,
    string? Detail);

public record CreditCustomerDetail(
    long Id,
    string Name,
    string? Phone,
    string? Notes,
    decimal Balance,
    IReadOnlyList<CreditHistoryEntry> History);
