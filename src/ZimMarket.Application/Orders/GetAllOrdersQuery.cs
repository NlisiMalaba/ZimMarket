using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Domain.Enums;
using ZimMarket.Shared;

namespace ZimMarket.Application.Orders;

public sealed record GetAllOrdersQuery(
    OrderStatus? Status,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    int Page,
    int PageSize) : IQuery<PagedList<AdminOrderListItemDto>>;
