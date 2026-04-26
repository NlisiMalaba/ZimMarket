using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Orders;

public sealed record GetSellerOrderDetailQuery(Guid OrderId) : IQuery<SellerOrderDetailDto>;

