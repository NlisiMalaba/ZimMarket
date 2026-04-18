using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Orders;

public sealed record PlaceOrderCommand(
    IReadOnlyList<PlaceOrderItemDto> Items,
    PlaceOrderDeliveryAddressDto DeliveryAddress,
    PaymentMethod PaymentMethod) : ICommand<PlaceOrderResultDto>;

public sealed record PlaceOrderItemDto(Guid ProductId, int Quantity);

public sealed record PlaceOrderDeliveryAddressDto(
    string Street,
    string Suburb,
    string City,
    string Country);
