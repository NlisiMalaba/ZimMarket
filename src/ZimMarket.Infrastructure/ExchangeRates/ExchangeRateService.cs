using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Infrastructure.Persistence;

namespace ZimMarket.Infrastructure.ExchangeRates;

/// <summary>
/// Resolves USD→ZWL (or legacy USD→ZWG) using Redis cache-aside, then the latest row in <c>exchange_rates</c>.
/// </summary>
public sealed class ExchangeRateService : IExchangeRateService
{
    /// <summary>Cache key from product design (24h TTL when populated by readers or the daily job).</summary>
    public const string UsdToZwlCacheKey = "exchange-rate:usd-zwl";

    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    /// <summary>Matches seeded default when no data is available.</summary>
    private const decimal FallbackRate = 26m;

    private readonly AppDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<ExchangeRateService> _logger;

    public ExchangeRateService(
        AppDbContext dbContext,
        ICacheService cache,
        ILogger<ExchangeRateService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<decimal> GetUsdToZwlAsync(CancellationToken cancellationToken = default)
    {
        decimal? cached = await _cache.GetAsync<decimal>(UsdToZwlCacheKey, cancellationToken).ConfigureAwait(false);
        if (cached is > 0)
            return cached.Value;

        decimal? fromDatabase = await TryReadLatestRateFromDatabaseAsync(cancellationToken).ConfigureAwait(false);
        if (fromDatabase is > 0)
        {
            try
            {
                await _cache
                    .SetAsync(UsdToZwlCacheKey, fromDatabase.Value, CacheTtl, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exchange rate cache write failed after database read.");
            }

            return fromDatabase.Value;
        }

        _logger.LogWarning(
            "No USD→ZWL/ZWG exchange rate in database; returning fallback {FallbackRate}.",
            FallbackRate);

        return FallbackRate;
    }

    private async Task<decimal?> TryReadLatestRateFromDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.ExchangeRates.AsNoTracking()
                .Where(x =>
                    x.BaseCurrency == "USD"
                    && (x.QuoteCurrency == "ZWL" || x.QuoteCurrency == "ZWG"))
                .OrderByDescending(x => x.EffectiveAt)
                .Select(x => (decimal?)x.Rate)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read exchange rate from database; returning fallback.");
            return null;
        }
    }
}
