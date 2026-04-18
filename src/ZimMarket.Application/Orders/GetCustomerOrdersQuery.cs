using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Orders;

public sealed record GetCustomerOrdersQuery(
    int Page,
    int PageSize,
    OrderStatus? StatusFilter) : IQuery<ZimMarket.Shared.PagedList<CustomerOrderListItemDto>>;
