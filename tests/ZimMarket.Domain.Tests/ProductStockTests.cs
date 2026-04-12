using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Exceptions;
using FluentAssertions;

namespace ZimMarket.Domain.Tests;

public class ProductStockTests
{
    [Fact]
    public void UpdateStock_negative_result_throws()
    {
        var product = CreateProduct(stockQuantity: 3);
        var act = () => product.UpdateStock(-4);
        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Fact]
    public void UpdateStock_depleting_from_positive_to_zero_raises_StockDepletedEvent()
    {
        var product = CreateProduct(stockQuantity: 2);
        product.UpdateStock(-2);

        product.StockQuantity.Should().Be(0);
        var events = product.PopDomainEvents();
        events.Should().ContainSingle(e => e is StockDepletedEvent);
        events.OfType<StockDepletedEvent>().Single().ProductId.Should().Be(product.Id);
    }

    [Fact]
    public void UpdateStock_when_already_zero_does_not_raise_StockDepletedEvent_again()
    {
        var product = CreateProduct(stockQuantity: 0);
        product.UpdateStock(0);
        product.PopDomainEvents();

        product.UpdateStock(0);
        product.PopDomainEvents().Should().BeEmpty();
    }

    private static Product CreateProduct(int stockQuantity)
    {
        var now = DateTimeOffset.UtcNow;
        return Product.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "T",
            "D",
            DomainTestHelpers.TenUsd,
            Guid.NewGuid(),
            stockQuantity,
            [],
            DomainTestHelpers.ValidAddress,
            now,
            now).Value!;
    }
}
