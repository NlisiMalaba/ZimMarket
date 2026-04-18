using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Shared;

namespace ZimMarket.Application.Catalogue;

public sealed record GetSellerProductsQuery(int Page, int PageSize) : IQuery<PagedList<ProductSummaryDto>>;
