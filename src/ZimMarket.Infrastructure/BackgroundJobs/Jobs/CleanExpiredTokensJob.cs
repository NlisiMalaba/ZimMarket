using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Infrastructure.Persistence;

namespace ZimMarket.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Nullifies expired refresh tokens without deleting user records.
/// </summary>
public sealed class CleanExpiredTokensJob
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CleanExpiredTokensJob> _logger;

    public CleanExpiredTokensJob(AppDbContext dbContext, ILogger<CleanExpiredTokensJob> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        List<User> expiredTokenUsers = await _dbContext.Set<User>()
            .Where(u => u.RefreshTokenHash != null && u.RefreshTokenExpiry != null && u.RefreshTokenExpiry < now)
            .ToListAsync()
            .ConfigureAwait(false);

        if (expiredTokenUsers.Count == 0)
        {
            _logger.LogInformation("CleanExpiredTokensJob completed: no expired refresh tokens found.");
            return;
        }

        foreach (User user in expiredTokenUsers)
        {
            user.ClearRefreshToken();
        }

        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation(
            "CleanExpiredTokensJob completed: nullified expired refresh tokens for {UserCount} users at {RunAtUtc}.",
            expiredTokenUsers.Count,
            now);
    }
}
