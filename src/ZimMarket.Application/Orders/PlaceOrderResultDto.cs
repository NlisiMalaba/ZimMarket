namespace ZimMarket.Application.Orders;

public sealed record PlaceOrderResultDto(
    Guid OrderId,
    decimal TotalUsd,
    decimal TotalZwl);
