namespace ZimMarket.Shared;

public sealed record PaginationParams
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? SortBy { get; init; }

    public string? SortDir { get; init; }
}
