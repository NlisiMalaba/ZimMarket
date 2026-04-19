using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Admin;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Admin;

public sealed class SuspendProductCommandHandlerTests
{
    [Fact]
    public async Task Non_admin_returns_forbidden()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Seller);

        var handler = new SuspendProductCommandHandler(
            unitOfWork,
            currentUser,
            Substitute.For<ICacheService>(),
            NullLogger<SuspendProductCommandHandler>.Instance);

        var result = await handler.Handle(
            new SuspendProductCommand(Guid.NewGuid(), "Spam"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AdminProductErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Admin_suspends_active_product_updates_and_invalidates_cache()
    {
        Guid productId = Guid.NewGuid();
        var product = CreateActiveProduct(productId);

        var products = Substitute.For<IProductRepository>();
        products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Products.Returns(products);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);

        var cache = Substitute.For<ICacheService>();

        var handler = new SuspendProductCommandHandler(
            unitOfWork,
            currentUser,
            cache,
            NullLogger<SuspendProductCommandHandler>.Instance);

        var result = await handler.Handle(
            new SuspendProductCommand(productId, "Counterfeit goods"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Status.Should().Be(ProductStatus.Suspended);
        product.SuspensionReason.Should().Be("Counterfeit goods");
        await products.Received(1).UpdateAsync(product, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveAsync($"product:{productId:D}", Arg.Any<CancellationToken>());
    }

    private static Product CreateActiveProduct(Guid productId)
    {
        var price = Money.Create(5m, Currency.USD).Value!;
        var address = Address.Create("10 Main St", "Avenues", "Harare", "Zimbabwe").Value!;
        var now = DateTimeOffset.UtcNow;
        return Product.Create(
            productId,
            Guid.NewGuid(),
            "Item",
            "Desc",
            price,
            Guid.NewGuid(),
            3,
            [],
            address,
            now,
            now).Value!;
    }
}
