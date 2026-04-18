using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Shared;

namespace ZimMarket.Application.Catalogue;

public sealed record SearchProductsQuery(
    string? SearchTerm,
    Guid? CategoryId,
    decimal? MinPriceUsd,
    decimal? MaxPriceUsd,
    int Page,
    int PageSize) : IQuery<PagedList<ProductSummaryDto>>;
