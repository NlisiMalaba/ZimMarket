namespace ZimMarket.Shared;

public sealed record PaginationParams
{
    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        init => _page = NormalizePage(value);
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = NormalizePageSize(value);
    }

    public string? SortBy { get; init; }

    public string? SortDir { get; init; }

    private static int NormalizePage(int value) => value < 1 ? 1 : value;

    private static int NormalizePageSize(int value)
    {
        if (value < 1)
            return 20;

        return value > 100 ? 100 : value;
    }
}
