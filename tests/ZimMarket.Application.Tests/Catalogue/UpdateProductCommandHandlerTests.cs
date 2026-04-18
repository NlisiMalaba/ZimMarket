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

public sealed class UpdateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_caller_not_owner_returns_products_forbidden()
    {
        var currentUser = CreateApprovedSellerCurrentUser();
        Guid ownerId = Guid.NewGuid();
        var command = CreateValidCommand();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var products = Substitute.For<IProductRepository>();
        unitOfWork.Products.Returns(products);
        products.GetByIdAsync(command.ProductId, Arg.Any<CancellationToken>()).Returns(CreateProduct(ownerId, command.ProductId));

        var handler = new UpdateProductCommandHandler(
            currentUser,
            unitOfWork,
            Substitute.For<IFileStorage>(),
            Substitute.For<ICacheService>(),
            NullLogger<UpdateProductCommandHandler>.Instance);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Products.Forbidden");
    }

    [Fact]
    public async Task Handle_valid_update_saves_and_invalidates_product_cache()
    {
        var currentUser = CreateApprovedSellerCurrentUser();
        var command = CreateValidCommand();
        var existingProduct = CreateProduct(currentUser.UserId, command.ProductId);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var products = Substitute.For<IProductRepository>();
        var categories = Substitute.For<ICategoryRepository>();
        unitOfWork.Products.Returns(products);
        unitOfWork.Categories.Returns(categories);
        products.GetByIdAsync(command.ProductId, Arg.Any<CancellationToken>()).Returns(existingProduct);
        categories.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        var fileStorage = Substitute.For<IFileStorage>();
        fileStorage.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var cache = Substitute.For<ICacheService>();
        var handler = new UpdateProductCommandHandler(
            currentUser,
            unitOfWork,
            fileStorage,
            cache,
            NullLogger<UpdateProductCommandHandler>.Instance);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await products.Received(1).UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
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

    private static Product CreateProduct(Guid sellerId, Guid productId)
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
            2,
            ["product-images/seller/original.jpg"],
            address,
            now,
            now).Value!;
    }

    private static UpdateProductCommand CreateValidCommand() =>
        new(
            Guid.NewGuid(),
            "Updated Tomatoes",
            "Updated product description",
            4.75m,
            Guid.NewGuid(),
            ["product-images/seller/updated.jpg"],
            new PickupAddressDto("25 Market Road", "Avondale", "Harare", "Zimbabwe"));
}
