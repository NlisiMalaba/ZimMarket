using Microsoft.Extensions.Logging;

namespace ZimMarket.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Removes expired refresh token rows (nightly maintenance).
/// </summary>
public sealed class CleanExpiredTokensJob
{
    private readonly ILogger<CleanExpiredTokensJob> _logger;

    public CleanExpiredTokensJob(ILogger<CleanExpiredTokensJob> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Execute()
    {
        _logger.LogInformation("CleanExpiredTokensJob invoked (implementation pending).");
    }
}
