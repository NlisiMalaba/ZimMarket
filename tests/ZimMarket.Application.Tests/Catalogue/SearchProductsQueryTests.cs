using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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
    public async Task Handler_text_filter_returns_matching_results()
    {
        Guid sellerId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        var result = await HandleQueryAsync(
            new SearchProductsQuery("tomato", categoryId, 1m, 10m, 1, 10),
            new PagedList<Product>(
            [CreateProduct(sellerId, productId, categoryId, "Fresh tomato", 5m)],
            page: 1,
            pageSize: 10,
            totalCount: 1));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(x => x.Title.Contains("tomato", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handler_price_range_filter_excludes_out_of_range_products()
    {
        Guid sellerId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();

        var result = await HandleQueryAsync(
            new SearchProductsQuery(null, categoryId, 3m, 6m, 1, 20),
            new PagedList<Product>(
            [
                CreateProduct(sellerId, Guid.NewGuid(), categoryId, "Range Match", 5m)
            ],
            page: 1,
            pageSize: 20,
            totalCount: 1));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.Should().OnlyContain(x => x.PriceAmount >= 3m && x.PriceAmount <= 6m);
    }

    [Fact]
    public async Task Handler_pagination_returns_correct_page()
    {
        Guid sellerId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();

        var result = await HandleQueryAsync(
            new SearchProductsQuery(null, null, null, null, 2, 5),
            new PagedList<Product>(
            [
                CreateProduct(sellerId, Guid.NewGuid(), categoryId, "P2 Item 1", 4m),
                CreateProduct(sellerId, Guid.NewGuid(), categoryId, "P2 Item 2", 5m)
            ],
            page: 2,
            pageSize: 5,
            totalCount: 12));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(5);
        result.Value.TotalCount.Should().Be(12);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Query_is_not_cacheable()
    {
        var query = new SearchProductsQuery(null, null, null, null, 1, 20);
        query.Should().NotBeAssignableTo<ZimMarket.Application.Common.Abstractions.ICacheable>();
    }

    [Fact]
    public async Task Handler_returns_products_without_primary_image_when_storage_is_unavailable()
    {
        Guid sellerId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        var repositoryResult = new PagedList<Product>(
            [CreateProduct(sellerId, Guid.NewGuid(), categoryId, "Fresh tomato", 5m)],
            page: 1,
            pageSize: 20,
            totalCount: 1);

        var result = await HandleQueryAsync(
            new SearchProductsQuery(null, null, null, null, 1, 20),
            repositoryResult,
            fileStorage =>
            {
                fileStorage.GenerateSasUrlAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
                    .Returns<Task<string>>(_ => throw new InvalidOperationException("File storage is not configured."));
            });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        result.Value.Items[0].PrimaryImageUrl.Should().BeNull();
    }

    private static async Task<ZimMarket.Application.Common.Models.Result<PagedList<ProductSummaryDto>>> HandleQueryAsync(
        SearchProductsQuery query,
        PagedList<Product> repositoryResult,
        Action<IFileStorage>? configureFileStorage = null)
    {
        Guid sellerId = repositoryResult.Items[0].SellerId;
        Guid categoryId = repositoryResult.Items[0].CategoryId;

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var products = Substitute.For<IProductRepository>();
        var categories = Substitute.For<ICategoryRepository>();
        var sellers = Substitute.For<IUserRepository<Seller>>();
        unitOfWork.Products.Returns(products);
        unitOfWork.Categories.Returns(categories);
        unitOfWork.Sellers.Returns(sellers);

        products.GetPagedAsync(
                Arg.Is<ProductFilter>(f =>
                    f.SearchTerm == query.SearchTerm &&
                    f.CategoryId == query.CategoryId &&
                    f.MinPriceUsd == query.MinPriceUsd &&
                    f.MaxPriceUsd == query.MaxPriceUsd &&
                    f.SellerId == null),
                Arg.Is<PaginationParams>(p => p.Page == query.Page && p.PageSize == query.PageSize),
                Arg.Any<CancellationToken>())
            .Returns(repositoryResult);

        sellers.GetByIdAsync(sellerId, Arg.Any<CancellationToken>())
            .Returns(CreateSeller(sellerId, "Acme Farms"));
        categories.GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(Category.Create(categoryId, "Vegetables", "vegetables", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow).Value!);

        var fileStorage = Substitute.For<IFileStorage>();
        fileStorage.GenerateSasUrlAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ci => $"https://blob.example/{ci.ArgAt<string>(0)}");
        configureFileStorage?.Invoke(fileStorage);

        var handler = new SearchProductsQueryHandler(
            unitOfWork,
            fileStorage,
            NullLogger<SearchProductsQueryHandler>.Instance);
        return await handler.Handle(query, CancellationToken.None);
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
