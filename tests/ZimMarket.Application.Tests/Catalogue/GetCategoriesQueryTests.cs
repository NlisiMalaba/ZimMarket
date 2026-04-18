using FluentAssertions;
using NSubstitute;
using ZimMarket.Application.Catalogue;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Application.Tests.Catalogue;

public sealed class GetCategoriesQueryTests
{
    [Fact]
    public void Query_implements_cache_contract()
    {
        var query = new GetCategoriesQuery();

        query.CacheKey.Should().Be("categories:all");
        query.Ttl.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task Handler_returns_all_categories_mapped()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var categoriesRepo = Substitute.For<ICategoryRepository>();
        unitOfWork.Categories.Returns(categoriesRepo);

        Category root = Category.Create(
            Guid.NewGuid(),
            "Vegetables",
            "vegetables",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;

        Category child = Category.Create(
            Guid.NewGuid(),
            "Leafy Greens",
            "leafy-greens",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            root).Value!;

        categoriesRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([root, child]);

        var handler = new GetCategoriesQueryHandler(unitOfWork);
        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Slug.Should().Be("vegetables");
        result.Value[1].ParentCategoryId.Should().Be(root.Id);
    }
}
