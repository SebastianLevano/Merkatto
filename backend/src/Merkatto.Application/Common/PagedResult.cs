namespace Merkatto.Application.Common;

/// <summary>A page of results plus the total count, for list endpoints.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
}

/// <summary>Common paging/search query for list endpoints.</summary>
public record PagedQuery
{
    private const int MaxPageSize = 100;
    public int Page { get; init; } = 1;

    private readonly int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value is < 1 or > MaxPageSize ? 20 : value;
    }

    public string? Search { get; init; }

    public int Skip => (Math.Max(Page, 1) - 1) * PageSize;
}
