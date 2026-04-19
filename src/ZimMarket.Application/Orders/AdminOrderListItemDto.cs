using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Orders;

public sealed record AdminOrderListItemDto(
    Guid OrderId,
    Guid CustomerId,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal TotalAmount,
    string TotalCurrency,
    int LineItemCount,
    DateTimeOffset CreatedAt);
