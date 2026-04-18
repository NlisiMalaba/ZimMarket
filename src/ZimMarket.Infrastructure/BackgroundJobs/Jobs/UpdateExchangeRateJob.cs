using Microsoft.Extensions.Logging;

namespace ZimMarket.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Fetches RBZ USD/ZWL rate and updates cache/DB (scheduled daily).
/// </summary>
public sealed class UpdateExchangeRateJob
{
    private readonly ILogger<UpdateExchangeRateJob> _logger;

    public UpdateExchangeRateJob(ILogger<UpdateExchangeRateJob> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Execute()
    {
        _logger.LogInformation("UpdateExchangeRateJob invoked (implementation pending IExchangeRateService).");
    }
}
