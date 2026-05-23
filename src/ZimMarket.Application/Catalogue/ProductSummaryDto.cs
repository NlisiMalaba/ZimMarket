using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Catalogue;

public sealed class ProductSummaryDto
{
    public required Guid ProductId { get; init; }

    public required ProductStatus Status { get; init; }

    public required string Title { get; init; }

    public required decimal PriceAmount { get; init; }

    public required string PriceCurrency { get; init; }

    public required int StockQuantity { get; init; }

    public required Guid SellerId { get; init; }

    public required string SellerName { get; init; }

    public required Guid CategoryId { get; init; }

    public required string CategoryName { get; init; }

    public string? PrimaryImageUrl { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
