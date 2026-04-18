using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Orders;

public sealed record CustomerOrderListItemDto(
    Guid OrderId,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal TotalUsd,
    DateTimeOffset CreatedAt);
