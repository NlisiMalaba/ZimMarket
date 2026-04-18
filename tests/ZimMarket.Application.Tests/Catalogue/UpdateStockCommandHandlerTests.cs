using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Catalogue;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Catalogue;

public sealed class UpdateStockCommandHandlerTests
{
    [Fact]
    public async Task Handle_caller_not_owner_returns_products_forbidden()
    {
        var currentUser = CreateApprovedSellerCurrentUser();
        Guid ownerId = Guid.NewGuid();
        var command = new UpdateStockCommand(Guid.NewGuid(), 3);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var products = Substitute.For<IProductRepository>();
        unitOfWork.Products.Returns(products);
        products.GetByIdAsync(command.ProductId, Arg.Any<CancellationToken>()).Returns(CreateProduct(ownerId, command.ProductId, 5));

        var handler = new UpdateStockCommandHandler(
            currentUser,
            unitOfWork,
            Substitute.For<ICacheService>(),
            NullLogger<UpdateStockCommandHandler>.Instance);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Products.Forbidden");
    }

    [Fact]
    public async Task Handle_valid_delta_updates_stock_and_invalidates_cache()
    {
        var currentUser = CreateApprovedSellerCurrentUser();
        var command = new UpdateStockCommand(Guid.NewGuid(), -2);
        var existingProduct = CreateProduct(currentUser.UserId, command.ProductId, 5);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var products = Substitute.For<IProductRepository>();
        unitOfWork.Products.Returns(products);
        products.GetByIdAsync(command.ProductId, Arg.Any<CancellationToken>()).Returns(existingProduct);

        var cache = Substitute.For<ICacheService>();
        var handler = new UpdateStockCommandHandler(
            currentUser,
            unitOfWork,
            cache,
            NullLogger<UpdateStockCommandHandler>.Instance);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existingProduct.StockQuantity.Should().Be(3);
        await products.Received(1).UpdateAsync(existingProduct, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveAsync($"product:{command.ProductId:D}", Arg.Any<CancellationToken>());
    }

    private static ICurrentUser CreateApprovedSellerCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Role.Returns(UserRole.Seller);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.GetClaim(AuthClaimTypes.KycStatus).Returns(KycStatus.Approved.ToString());
        return currentUser;
    }

    private static Product CreateProduct(Guid sellerId, Guid productId, int stockQuantity)
    {
        var price = Money.Create(10m, Currency.USD).Value!;
        var address = Address.Create("10 First Ave", "Borrowdale", "Harare", "Zimbabwe").Value!;
        var now = DateTimeOffset.UtcNow;
        return Product.Create(
            productId,
            sellerId,
            "Original",
            "Original description",
            price,
            Guid.NewGuid(),
            stockQuantity,
            ["product-images/seller/original.jpg"],
            address,
            now,
            now).Value!;
    }
}
