using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Orders;

public sealed record SellerOrderListItemDto(
    Guid OrderId,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal TotalUsd,
    decimal SellerTotalUsd,
    int SellerLineItemCount,
    DateTimeOffset CreatedAt,
    string CustomerName,
    string CustomerEmail,
    string PrimaryProductTitle);

