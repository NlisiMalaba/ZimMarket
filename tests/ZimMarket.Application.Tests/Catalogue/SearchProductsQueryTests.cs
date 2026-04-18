using FluentAssertions;
using NSubstitute;
using ZimMarket.Application.Catalogue;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Common;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;
using ZimMarket.Shared;

namespace ZimMarket.Application.Tests.Catalogue;

public sealed class SearchProductsQueryTests
{
    [Fact]
    public async Task Handler_applies_filters_and_returns_paged_summary()
    {
        Guid sellerId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        var query = new SearchProductsQuery("tomato", categoryId, 1m, 10m, 2, 5);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var products = Substitute.For<IProductRepository>();
        var categories = Substitute.For<ICategoryRepository>();
        var sellers = Substitute.For<IUserRepository<Seller>>();
        unitOfWork.Products.Returns(products);
        unitOfWork.Categories.Returns(categories);
        unitOfWork.Sellers.Returns(sellers);

        var pagedProducts = new PagedList<Product>(
            [CreateProduct(sellerId, productId, categoryId, "Fresh tomato", 5m)],
            page: 2,
            pageSize: 5,
            totalCount: 11);

        products.GetPagedAsync(
                Arg.Is<ProductFilter>(f =>
                    f.SearchTerm == "tomato" &&
                    f.CategoryId == categoryId &&
                    f.MinPriceUsd == 1m &&
                    f.MaxPriceUsd == 10m &&
                    f.SellerId == null),
                Arg.Is<PaginationParams>(p => p.Page == 2 && p.PageSize == 5),
                Arg.Any<CancellationToken>())
            .Returns(pagedProducts);

        sellers.GetByIdAsync(sellerId, Arg.Any<CancellationToken>())
            .Returns(CreateSeller(sellerId, "Acme Farms"));
        categories.GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(Category.Create(categoryId, "Vegetables", "vegetables", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow).Value!);

        var fileStorage = Substitute.For<IFileStorage>();
        fileStorage.GenerateSasUrlAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ci => $"https://blob.example/{ci.ArgAt<string>(0)}");

        var handler = new SearchProductsQueryHandler(unitOfWork, fileStorage);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(5);
        result.Value.TotalCount.Should().Be(11);
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].SellerName.Should().Be("Acme Farms");
        result.Value.Items[0].CategoryName.Should().Be("Vegetables");
        result.Value.Items[0].PrimaryImageUrl.Should().Contain("product-images/seller/image-1.jpg");
    }

    [Fact]
    public void Query_is_not_cacheable()
    {
        var query = new SearchProductsQuery(null, null, null, null, 1, 20);
        query.Should().NotBeAssignableTo<ZimMarket.Application.Common.Abstractions.ICacheable>();
    }

    private static Product CreateProduct(Guid sellerId, Guid productId, Guid categoryId, string title, decimal priceUsd)
    {
        var price = Money.Create(priceUsd, Currency.USD).Value!;
        var address = Address.Create("10 Main St", "Avenues", "Harare", "Zimbabwe").Value!;
        var now = DateTimeOffset.UtcNow;
        return Product.Create(
            productId,
            sellerId,
            title,
            "Organic produce",
            price,
            categoryId,
            8,
            ["product-images/seller/image-1.jpg"],
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
