using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Catalogue;

public sealed record CreateProductCommand(
    string Title,
    string Description,
    decimal PriceUsd,
    Guid CategoryId,
    int StockQuantity,
    IReadOnlyList<string> ImageKeys,
    PickupAddressDto PickupAddress) : ICommand<Guid>;

public sealed record PickupAddressDto(
    string Street,
    string Suburb,
    string City,
    string Country);
