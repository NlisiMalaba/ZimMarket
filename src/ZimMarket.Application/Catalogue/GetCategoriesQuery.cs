using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Catalogue;

public sealed record GetCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>, ICacheable
{
    public string CacheKey => "categories:all";

    public TimeSpan Ttl => TimeSpan.FromHours(1);
}
