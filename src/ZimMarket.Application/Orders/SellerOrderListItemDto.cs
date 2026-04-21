using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Orders;

public sealed record SellerOrderListItemDto(
    Guid OrderId,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal TotalUsd,
    int SellerLineItemCount,
    DateTimeOffset CreatedAt);

