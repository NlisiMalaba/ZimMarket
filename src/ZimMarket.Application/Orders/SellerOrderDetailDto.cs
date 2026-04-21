using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Orders;

public sealed record SellerOrderDetailDto(
    Guid OrderId,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal TotalUsd,
    string CustomerCity,
    IReadOnlyList<SellerOrderDetailItemDto> Items);

public sealed record SellerOrderDetailItemDto(
    Guid ProductId,
    string ProductTitle,
    int Quantity,
    decimal UnitPriceUsd,
    decimal LineTotalUsd);

