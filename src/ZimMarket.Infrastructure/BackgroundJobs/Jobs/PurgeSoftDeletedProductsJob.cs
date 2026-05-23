using Microsoft.Extensions.Logging;
using ZimMarket.Application.Catalogue;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Permanently removes products that have been soft-deleted for longer than the retention window.
/// Product images are removed at soft-delete time; this job only purges database rows.
/// </summary>
public sealed class PurgeSoftDeletedProductsJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PurgeSoftDeletedProductsJob> _logger;

    public PurgeSoftDeletedProductsJob(IUnitOfWork unitOfWork, ILogger<PurgeSoftDeletedProductsJob> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync()
    {
        DateTimeOffset deletedBeforeUtc = DateTimeOffset.UtcNow.AddDays(-CatalogueConstants.DeletedProductRetentionDays);
        IReadOnlyList<Product> expiredProducts = await _unitOfWork.Products
            .FindSoftDeletedOlderThanAsync(deletedBeforeUtc, CancellationToken.None)
            .ConfigureAwait(false);

        if (expiredProducts.Count == 0)
        {
            _logger.LogInformation(
                "PurgeSoftDeletedProductsJob completed: no products deleted before {DeletedBeforeUtc}.",
                deletedBeforeUtc);
            return;
        }

        foreach (Product product in expiredProducts)
        {
            await _unitOfWork.Products.HardDeleteAsync(product.Id, CancellationToken.None).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        _logger.LogInformation(
            "PurgeSoftDeletedProductsJob permanently removed {ProductCount} soft-deleted products older than {RetentionDays} days.",
            expiredProducts.Count,
            CatalogueConstants.DeletedProductRetentionDays);
    }
}
