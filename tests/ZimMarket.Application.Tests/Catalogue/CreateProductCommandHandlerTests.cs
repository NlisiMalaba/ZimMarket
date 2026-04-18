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

namespace ZimMarket.Application.Tests.Catalogue;

public sealed class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_non_approved_seller_returns_products_forbidden()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Role.Returns(UserRole.Seller);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.GetClaim(AuthClaimTypes.KycStatus).Returns(KycStatus.PendingReview.ToString());

        var handler = new CreateProductCommandHandler(
            currentUser,
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IFileStorage>(),
            NullLogger<CreateProductCommandHandler>.Instance);

        var result = await handler.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Products.Forbidden");
    }

    [Fact]
    public async Task Handle_unknown_category_returns_validation_error()
    {
        var currentUser = CreateApprovedSellerCurrentUser();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var categories = Substitute.For<ICategoryRepository>();
        unitOfWork.Categories.Returns(categories);
        categories.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateProductCommandHandler(
            currentUser,
            unitOfWork,
            Substitute.For<IFileStorage>(),
            NullLogger<CreateProductCommandHandler>.Instance);

        var result = await handler.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainSingle(x => x.Field == nameof(CreateProductCommand.CategoryId));
    }

    [Fact]
    public async Task Handle_valid_request_adds_product_and_returns_id()
    {
        var currentUser = CreateApprovedSellerCurrentUser();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var categories = Substitute.For<ICategoryRepository>();
        var products = Substitute.For<IProductRepository>();
        unitOfWork.Categories.Returns(categories);
        unitOfWork.Products.Returns(products);
        categories.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        var fileStorage = Substitute.For<IFileStorage>();
        fileStorage.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateProductCommandHandler(
            currentUser,
            unitOfWork,
            fileStorage,
            NullLogger<CreateProductCommandHandler>.Instance);

        var result = await handler.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await products.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

    private static CreateProductCommand CreateValidCommand() =>
        new(
            "Fresh Tomatoes",
            "Farm fresh tomatoes.",
            3.25m,
            Guid.NewGuid(),
            12,
            ["product-images/seller/image-1.jpg"],
            new PickupAddressDto("123 Main Street", "Avondale", "Harare", "Zimbabwe"));
}
