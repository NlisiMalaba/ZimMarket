using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Domain.Enums;
using ZimMarket.Shared;

namespace ZimMarket.Application.Orders;

public sealed record GetSellerOrdersQuery(
    int Page,
    int PageSize,
    OrderStatus? StatusFilter,
    SellerOrderStatusGroup? StatusGroup) : IQuery<PagedList<SellerOrderListItemDto>>;

