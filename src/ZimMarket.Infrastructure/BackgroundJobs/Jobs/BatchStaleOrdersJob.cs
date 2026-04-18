using Microsoft.Extensions.Logging;

namespace ZimMarket.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Flags unpaid orders older than the SLA for cancellation (periodic sweep).
/// </summary>
public sealed class BatchStaleOrdersJob
{
    private readonly ILogger<BatchStaleOrdersJob> _logger;

    public BatchStaleOrdersJob(ILogger<BatchStaleOrdersJob> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Execute()
    {
        _logger.LogInformation("BatchStaleOrdersJob invoked (implementation pending).");
    }
}
