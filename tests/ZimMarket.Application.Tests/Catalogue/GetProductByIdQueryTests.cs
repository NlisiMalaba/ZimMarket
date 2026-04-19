using FluentAssertions;
using NSubstitute;
using ZimMarket.Application.Catalogue;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Catalogue;

public sealed class GetProductByIdQueryTests
{
    [Fact]
    public void Query_implements_cache_key_and_ttl_contract()
    {
        Guid productId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var query = new GetProductByIdQuery(productId);

        query.CacheKey.Should().Be($"product:{productId:D}");
        query.Ttl.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public async Task Handler_returns_product_details_with_image_urls()
    {
        Guid sellerId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var products = Substitute.For<IProductRepository>();
        var categories = Substitute.For<ICategoryRepository>();
        var sellers = Substitute.For<IUserRepository<Seller>>();
        unitOfWork.Products.Returns(products);
        unitOfWork.Categories.Returns(categories);
        unitOfWork.Sellers.Returns(sellers);

        var product = CreateProduct(sellerId, productId, categoryId);
        var category = Category.Create(categoryId, "Vegetables", "vegetables", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow).Value!;
        var seller = CreateSeller(sellerId, "Acme Farms");

        products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);
        categories.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns(category);
        sellers.GetByIdAsync(sellerId, Arg.Any<CancellationToken>()).Returns(seller);

        var fileStorage = Substitute.For<IFileStorage>();
        fileStorage.GenerateSasUrlAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => $"https://blob.example/{callInfo.ArgAt<string>(0)}");

        var handler = new GetProductByIdQueryHandler(unitOfWork, fileStorage);
        var result = await handler.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ProductId.Should().Be(productId);
        result.Value.SellerName.Should().Be("Acme Farms");
        result.Value.CategoryName.Should().Be("Vegetables");
        result.Value.ImageUrls.Should().HaveCount(2);
        result.Value.ImageUrls.Should().Contain(url => url.Contains("image-1.jpg", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handler_returns_not_found_when_product_suspended()
    {
        Guid sellerId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var products = Substitute.For<IProductRepository>();
        unitOfWork.Products.Returns(products);

        Product product = CreateProduct(sellerId, productId, categoryId);
        product.Suspend("Policy violation");
        products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new GetProductByIdQueryHandler(unitOfWork, Substitute.For<IFileStorage>());
        var result = await handler.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Products.NotFound");
    }

    private static Product CreateProduct(Guid sellerId, Guid productId, Guid categoryId)
    {
        var price = Money.Create(5m, Currency.USD).Value!;
        var address = Address.Create("10 Main St", "Avenues", "Harare", "Zimbabwe").Value!;
        var now = DateTimeOffset.UtcNow;
        return Product.Create(
            productId,
            sellerId,
            "Fresh carrots",
            "Organic carrots",
            price,
            categoryId,
            8,
            ["product-images/seller/image-1.jpg", "product-images/seller/image-2.jpg"],
            address,
            now,
            now).Value!;
    }

    private static Seller CreateSeller(Guid sellerId, string businessName)
    {
        var phone = PhoneNumber.Create("+263771112223").Value!;
        var now = DateTimeOffset.UtcNow;
        return new Seller(
            sellerId,
            "seller@example.com",
            "Seller Name",
            phone,
            "hash",
            KycStatus.Approved,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            createdAt: now,
            updatedAt: now,
            businessName,
            nationalIdDocumentKey: "nid",
            proofOfResidenceDocumentKey: "por",
            isApproved: true,
            rejectionReason: null);
    }
}
