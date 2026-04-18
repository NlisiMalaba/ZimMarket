using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Orders;

public sealed record OrderDetailDto(
    Guid OrderId,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    Guid? DeliveryBatchId,
    IReadOnlyList<OrderDetailItemDto> Items,
    decimal TotalUsd);

public sealed record OrderDetailItemDto(
    Guid ProductId,
    string ProductTitle,
    int Quantity,
    decimal UnitPriceUsd,
    decimal LineTotalUsd);
