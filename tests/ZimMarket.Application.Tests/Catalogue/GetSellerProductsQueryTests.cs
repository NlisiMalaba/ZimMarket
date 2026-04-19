using FluentAssertions;
using NSubstitute;
using ZimMarket.Application.Catalogue;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Common;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;
using ZimMarket.Shared;

namespace ZimMarket.Application.Tests.Catalogue;

public sealed class GetSellerProductsQueryTests
{
    [Fact]
    public async Task Handler_non_seller_returns_products_forbidden()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Role.Returns(UserRole.Customer);
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new GetSellerProductsQueryHandler(
            currentUser,
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IFileStorage>());

        var result = await handler.Handle(new GetSellerProductsQuery(1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Products.Forbidden");
    }

    [Fact]
    public async Task Handler_returns_seller_products_with_requested_paging()
    {
        Guid sellerId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Role.Returns(UserRole.Seller);
        currentUser.UserId.Returns(sellerId);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var products = Substitute.For<IProductRepository>();
        var categories = Substitute.For<ICategoryRepository>();
        unitOfWork.Products.Returns(products);
        unitOfWork.Categories.Returns(categories);

        products.GetPagedAsync(
                Arg.Is<ProductFilter>(f =>
                    f.SellerId == sellerId &&
                    f.SearchTerm == null &&
                    f.CategoryId == null &&
                    f.MinPriceUsd == null &&
                    f.MaxPriceUsd == null),
                Arg.Is<PaginationParams>(p => p.Page == 2 && p.PageSize == 10),
                Arg.Any<CancellationToken>())
            .Returns(new PagedList<Product>(
                [CreateProduct(sellerId, productId, categoryId, ProductStatus.Suspended)],
                page: 2,
                pageSize: 10,
                totalCount: 1));

        categories.GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(Category.Create(categoryId, "Vegetables", "vegetables", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow).Value!);

        var fileStorage = Substitute.For<IFileStorage>();
        fileStorage.GenerateSasUrlAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ci => $"https://blob.example/{ci.ArgAt<string>(0)}");

        var handler = new GetSellerProductsQueryHandler(currentUser, unitOfWork, fileStorage);
        var result = await handler.Handle(new GetSellerProductsQuery(2, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].SellerId.Should().Be(sellerId);
        result.Value.Items[0].Status.Should().Be(ProductStatus.Suspended);
    }

    private static Product CreateProduct(Guid sellerId, Guid productId, Guid categoryId, ProductStatus status)
    {
        var price = Money.Create(8m, Currency.USD).Value!;
        var address = Address.Create("10 Main St", "Avenues", "Harare", "Zimbabwe").Value!;
        var now = DateTimeOffset.UtcNow;
        var product = Product.Create(
            productId,
            sellerId,
            "Seller Product",
            "Seller inventory item",
            price,
            categoryId,
            4,
            ["product-images/seller/image-1.jpg"],
            address,
            now,
            now).Value!;

        if (status == ProductStatus.Suspended)
            product.Suspend("Policy test suspension");

        return product;
    }
}
