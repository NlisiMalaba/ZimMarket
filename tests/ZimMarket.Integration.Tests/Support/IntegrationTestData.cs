using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.ValueObjects;
using ZimMarket.Infrastructure.Persistence;
using ZimMarket.Shared;

namespace ZimMarket.Integration.Tests.Support;

internal static class IntegrationTestData
{
    public static async Task<(Guid SellerId, Guid CategoryId)> SeedSellerAndCategoryAsync(AppDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid sellerId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();

        string email = $"{Guid.NewGuid():N}@integration.test";
        Result<PhoneNumber> phoneResult = PhoneNumber.Create(
            $"+2637{Random.Shared.Next(10000000, 99999999):D8}");

        if (phoneResult.IsFailure)
            throw new InvalidOperationException(string.Join("; ", phoneResult.Errors));

        var seller = new Seller(
            sellerId,
            email,
            phoneResult.Value!,
            passwordHash: "integration-test-hash",
            KycStatus.Approved,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            createdAt: now,
            updatedAt: now,
            businessName: $"Biz-{Guid.NewGuid():N}",
            nationalIdDocumentKey: "kyc-nid-integration",
            proofOfResidenceDocumentKey: "kyc-por-integration",
            isApproved: true,
            rejectionReason: null);

        string slug = "t" + Guid.NewGuid().ToString("n");
        Result<Category> categoryResult = Category.Create(
            categoryId,
            $"Category-{categoryId:N}",
            slug,
            now,
            now);

        if (categoryResult.IsFailure)
            throw new InvalidOperationException(string.Join("; ", categoryResult.Errors));

        db.Add(seller);
        db.Add(categoryResult.Value!);
        await db.SaveChangesAsync().ConfigureAwait(false);

        return (sellerId, categoryId);
    }
}
