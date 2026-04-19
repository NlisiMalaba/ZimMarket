using FluentAssertions;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;

namespace ZimMarket.Domain.Tests;

public sealed class ProductSuspendTests
{
    [Fact]
    public void Suspend_sets_status_reason_and_allows_reason_update_when_already_suspended()
    {
        var product = CreateActiveProduct();
        product.Suspend("First reason");

        product.Status.Should().Be(ProductStatus.Suspended);
        product.SuspensionReason.Should().Be("First reason");

        product.Suspend("  Updated reason  ");
        product.SuspensionReason.Should().Be("Updated reason");
        product.Status.Should().Be(ProductStatus.Suspended);
    }

    [Fact]
    public void Suspend_empty_reason_throws()
    {
        var product = CreateActiveProduct();
        var act = () => product.Suspend("   ");
        act.Should().Throw<DomainException>().WithMessage("*reason*");
    }

    [Fact]
    public void Suspend_deleted_throws()
    {
        var product = CreateActiveProduct();
        product.Delete();

        var act = () => product.Suspend("x");
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Restore_clears_suspension_reason()
    {
        var product = CreateActiveProduct();
        product.Suspend("blocked");
        product.Restore();

        product.Status.Should().Be(ProductStatus.Active);
        product.SuspensionReason.Should().BeNull();
    }

    private static Product CreateActiveProduct()
    {
        var now = DateTimeOffset.UtcNow;
        return Product.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "T",
            "D",
            DomainTestHelpers.TenUsd,
            Guid.NewGuid(),
            1,
            [],
            DomainTestHelpers.ValidAddress,
            now,
            now).Value!;
    }
}
