using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Catalogue;

public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDetailDto>, ICacheable
{
    public string CacheKey => $"product:{ProductId:D}";

    public TimeSpan Ttl => TimeSpan.FromMinutes(10);
}
