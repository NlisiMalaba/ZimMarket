using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Domain.Enums;
using ZimMarket.Shared;

namespace ZimMarket.Application.Catalogue;

public sealed record GetSellerProductsQuery(
    int Page,
    int PageSize,
    SellerProductListScope Scope = SellerProductListScope.Active) : IQuery<PagedList<ProductSummaryDto>>;
