using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Catalogue;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Title,
    string Description,
    decimal PriceUsd,
    Guid CategoryId,
    IReadOnlyList<string> ImageKeys,
    PickupAddressDto PickupAddress) : ICommand;
