using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Entities;
using ZimMarket.Infrastructure.Configuration;
using ZimMarket.Infrastructure.ExchangeRates;
using ZimMarket.Infrastructure.Persistence;
using ZimMarket.Shared;

namespace ZimMarket.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Fetches RBZ USD/ZWL rate and updates cache/DB (scheduled daily).
/// </summary>
public sealed class UpdateExchangeRateJob
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly AppDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly IUsdZwlRateProvider _rateProvider;
    private readonly ExchangeRateProviderOptions _options;
    private readonly ILogger<UpdateExchangeRateJob> _logger;

    public UpdateExchangeRateJob(
        AppDbContext dbContext,
        ICacheService cache,
        IUsdZwlRateProvider rateProvider,
        IOptions<ExchangeRateProviderOptions> options,
        ILogger<UpdateExchangeRateJob> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _rateProvider = rateProvider ?? throw new ArgumentNullException(nameof(rateProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync()
    {
        decimal oldRate = await _dbContext.ExchangeRates.AsNoTracking()
            .Where(x => x.BaseCurrency == "USD" && x.QuoteCurrency == "ZWL")
            .OrderByDescending(x => x.EffectiveAt)
            .Select(x => x.Rate)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        decimal newRate = await ResolveRateAsync().ConfigureAwait(false);

        ExchangeRate? existing = await _dbContext.ExchangeRates
            .Where(x => x.BaseCurrency == "USD" && x.QuoteCurrency == "ZWL")
            .OrderByDescending(x => x.EffectiveAt)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            var created = ExchangeRate.Create(
                id: Guid.NewGuid(),
                baseCurrency: "USD",
                quoteCurrency: "ZWL",
                rate: newRate,
                effectiveAt: now,
                createdAt: now,
                updatedAt: now);

            if (created.IsFailure)
            {
                _logger.LogError("Unable to create exchange rate record: {Reason}", string.Join("; ", created.Errors));
                return;
            }

            await _dbContext.ExchangeRates.AddAsync(created.Value!).ConfigureAwait(false);
        }
        else
        {
            Result<ExchangeRate> updateResult = existing.UpdateRate(newRate, now);
            if (updateResult.IsFailure)
            {
                _logger.LogError("Unable to update exchange rate record {RateId}: {Reason}", existing.Id, string.Join("; ", updateResult.Errors));
                return;
            }
        }

        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        try
        {
            await _cache
                .SetAsync(ExchangeRateService.UsdToZwlCacheKey, newRate, CacheTtl)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "USD/ZWL cache update failed after database upsert.");
        }

        _logger.LogInformation(
            "USD/ZWL exchange rate updated successfully. Old={OldRate}, New={NewRate}, EffectiveAtUtc={EffectiveAtUtc}.",
            oldRate,
            newRate,
            now);
    }

    private async Task<decimal> ResolveRateAsync()
    {
        decimal? fetched = await _rateProvider.GetUsdToZwlAsync().ConfigureAwait(false);
        if (fetched is > 0)
            return fetched.Value;

        if (_options.FallbackUsdToZwlRate > 0)
        {
            _logger.LogWarning(
                "Falling back to configured USD/ZWL rate {FallbackRate} because providers are unavailable.",
                _options.FallbackUsdToZwlRate);
            return _options.FallbackUsdToZwlRate;
        }

        return ExchangeRateServiceFallbackRate();
    }

    private static decimal ExchangeRateServiceFallbackRate() => 26m;
}
