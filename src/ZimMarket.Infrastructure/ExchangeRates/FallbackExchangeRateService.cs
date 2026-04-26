using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Infrastructure.Configuration;

namespace ZimMarket.Infrastructure.ExchangeRates;

/// <summary>
/// Non-persistence fallback for DI-safe startup when database infrastructure is disabled.
/// </summary>
public sealed class FallbackExchangeRateService : IExchangeRateService
{
    private readonly ExchangeRateProviderOptions _options;
    private readonly ILogger<FallbackExchangeRateService> _logger;

    public FallbackExchangeRateService(
        IOptions<ExchangeRateProviderOptions> options,
        ILogger<FallbackExchangeRateService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<decimal> GetUsdToZwlAsync(CancellationToken cancellationToken = default)
    {
        var rate = _options.FallbackUsdToZwlRate > 0 ? _options.FallbackUsdToZwlRate : 26m;

        _logger.LogWarning(
            "Using fallback USD->ZWL rate {Rate} because database-backed exchange rate service is unavailable.",
            rate);

        return Task.FromResult(rate);
    }
}
