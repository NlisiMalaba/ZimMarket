using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Orders;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDetailDto>;
