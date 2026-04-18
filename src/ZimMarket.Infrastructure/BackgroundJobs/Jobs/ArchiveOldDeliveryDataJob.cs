using Microsoft.Extensions.Logging;

namespace ZimMarket.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Archives completed delivery batches (weekly).
/// </summary>
public sealed class ArchiveOldDeliveryDataJob
{
    private readonly ILogger<ArchiveOldDeliveryDataJob> _logger;

    public ArchiveOldDeliveryDataJob(ILogger<ArchiveOldDeliveryDataJob> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Execute()
    {
        _logger.LogInformation("ArchiveOldDeliveryDataJob invoked (implementation pending).");
    }
}
