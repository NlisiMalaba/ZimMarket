using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Catalogue;

public sealed class SellerProductDetailDto
{
    public required Guid ProductId { get; init; }

    public required ProductStatus Status { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required decimal PriceAmount { get; init; }

    public required string PriceCurrency { get; init; }

    public required int StockQuantity { get; init; }

    public required Guid CategoryId { get; init; }

    public required string CategoryName { get; init; }

    public required string PickupStreet { get; init; }

    public required string PickupSuburb { get; init; }

    public required string PickupCity { get; init; }

    public required string PickupCountry { get; init; }

    public required IReadOnlyList<string> ImageKeys { get; init; }

    public required IReadOnlyList<string> ImageUrls { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
