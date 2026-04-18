using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Infrastructure.Persistence;
using ZimMarket.Integration.Tests.Fixtures;
using ZimMarket.Shared;

namespace ZimMarket.Integration.Tests;

[Collection("Postgres")]
public sealed class UnitOfWorkTransactionIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public UnitOfWorkTransactionIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Rollback_after_failure_leaves_database_unchanged()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid categoryId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string slug = "t" + Guid.NewGuid().ToString("n");

        Result<Category> categoryResult = Category.Create(
            categoryId,
            "Rollback test category",
            slug,
            now,
            now);

        categoryResult.IsSuccess.Should().BeTrue();

        IExecutionStrategy strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await uow.BeginTransactionAsync();
            try
            {
                db.Add(categoryResult.Value!);
                await db.SaveChangesAsync();
                throw new InvalidOperationException("Simulated mid-command failure.");
            }
            catch (InvalidOperationException)
            {
                await uow.RollbackAsync();
            }
        });

        await using AsyncServiceScope verifyScope = _fixture.Services.CreateAsyncScope();
        AppDbContext dbVerify = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        bool exists = await dbVerify.Categories.AnyAsync(c => c.Id == categoryId);
        exists.Should().BeFalse();
    }
}
