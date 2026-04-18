using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.ValueObjects;
using ZimMarket.Infrastructure.Persistence;
using ZimMarket.Integration.Tests.Fixtures;
using ZimMarket.Integration.Tests.Support;
using ZimMarket.Shared;

namespace ZimMarket.Integration.Tests;

[Collection("Postgres")]
public sealed class ProductRepositoryIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public ProductRepositoryIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Product_lifecycle_add_get_update_stock_soft_delete_and_query_filter()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        (Guid sellerId, Guid categoryId) = await IntegrationTestData.SeedSellerAndCategoryAsync(db);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid productId = Guid.NewGuid();

        Result<Money> money = Money.Create(24.50m, Currency.USD);
        Result<Address> address = Address.Create("10 Vendor Road", "Suburb", "Bulawayo", "ZW");
        money.IsSuccess.Should().BeTrue();
        address.IsSuccess.Should().BeTrue();

        Result<Product> created = Product.Create(
            productId,
            sellerId,
            "Integration widget",
            "A widget created by integration tests for repository verification.",
            money.Value!,
            categoryId,
            stockQuantity: 10,
            imageKeys: ["product-images/integration-1.webp"],
            address.Value!,
            now,
            now);

        created.IsSuccess.Should().BeTrue();

        await uow.Products.AddAsync(created.Value!);
        await uow.SaveChangesAsync();

        Product? loaded = await uow.Products.GetByIdAsync(productId);
        loaded.Should().NotBeNull();
        loaded!.StockQuantity.Should().Be(10);

        Product tracked = await db.Products.FirstAsync(p => p.Id == productId);
        tracked.UpdateStock(7);
        await db.SaveChangesAsync();

        Product? afterStock = await uow.Products.GetByIdAsync(productId);
        afterStock.Should().NotBeNull();
        afterStock!.StockQuantity.Should().Be(17);

        tracked.Delete();
        await db.SaveChangesAsync();

        (await uow.Products.GetByIdAsync(productId)).Should().BeNull();

        bool stillPersisted = await db.Products
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Id == productId && p.Status == ProductStatus.Deleted);

        stillPersisted.Should().BeTrue();
    }
}
