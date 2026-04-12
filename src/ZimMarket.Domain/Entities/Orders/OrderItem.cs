using ZimMarket.Domain.ValueObjects;
using ZimMarket.Shared;

namespace ZimMarket.Domain.Entities.Orders;

public sealed class OrderItem
{
    private OrderItem(Guid productId, string productTitle, Money unitPrice, int quantity)
    {
        ProductId = productId;
        ProductTitle = productTitle;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid ProductId { get; }

    public string ProductTitle { get; }

    public Money UnitPrice { get; }

    public int Quantity { get; }

    public Money LineTotal
    {
        get
        {
            var amount = decimal.Round(UnitPrice.Amount * Quantity, 2, MidpointRounding.AwayFromZero);
            var result = Money.Create(amount, UnitPrice.Currency);
            if (result.IsFailure)
                throw new InvalidOperationException(string.Join("; ", result.Errors));

            return result.Value!;
        }
    }

    public static Result<OrderItem> Create(Guid productId, string productTitle, Money unitPrice, int quantity)
    {
        if (productId == Guid.Empty)
            return Result<OrderItem>.Failure("Product id is required.");

        if (string.IsNullOrWhiteSpace(productTitle))
            return Result<OrderItem>.Failure("Product title is required.");

        ArgumentNullException.ThrowIfNull(unitPrice);

        if (quantity <= 0)
            return Result<OrderItem>.Failure("Quantity must be greater than zero.");

        return Result<OrderItem>.Success(new OrderItem(productId, productTitle.Trim(), unitPrice, quantity));
    }
}
