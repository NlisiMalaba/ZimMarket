using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZimMarket.Infrastructure.Configuration;

namespace ZimMarket.Infrastructure.ExchangeRates;

/// <summary>
/// Fetches USD/ZWL from a configurable RBZ-compatible JSON endpoint.
/// </summary>
public sealed class RbzUsdZwlRateProvider : IUsdZwlRateProvider
{
    private static readonly string[] RateFieldCandidates = ["rate", "mid", "value", "buy", "ask", "selling"];

    private readonly HttpClient _httpClient;
    private readonly ExchangeRateProviderOptions _options;
    private readonly ILogger<RbzUsdZwlRateProvider> _logger;

    public RbzUsdZwlRateProvider(
        HttpClient httpClient,
        IOptions<ExchangeRateProviderOptions> options,
        ILogger<RbzUsdZwlRateProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<decimal?> GetUsdToZwlAsync(CancellationToken cancellationToken = default)
    {
        decimal? primary = await FetchFromUrlAsync(_options.PrimaryUrl, "primary", cancellationToken).ConfigureAwait(false);
        if (primary is > 0)
            return primary;

        return await FetchFromUrlAsync(_options.FallbackUrl, "fallback", cancellationToken).ConfigureAwait(false);
    }

    private async Task<decimal?> FetchFromUrlAsync(string url, string providerName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "USD/ZWL {Provider} provider returned non-success status code {StatusCode}.",
                    providerName,
                    (int)response.StatusCode);

                return null;
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            decimal? parsed = TryExtractUsdZwlRate(json.RootElement);
            if (parsed is > 0)
                return parsed;

            _logger.LogWarning("USD/ZWL {Provider} provider response did not contain a parseable rate.", providerName);
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "USD/ZWL {Provider} provider request failed.", providerName);
            return null;
        }
    }

    private static decimal? TryExtractUsdZwlRate(JsonElement element)
    {
        // Supported shapes:
        // 1) { "base":"USD", "quote":"ZWL", "rate": 26.5 }
        // 2) { "USD_ZWL": 26.5 }
        // 3) nested objects/arrays where an object includes USD+ZWL metadata and any known rate field.
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (TryExtractFromPairObject(element, out decimal directPairRate))
                return directPairRate;

            if (TryGetPropertyIgnoreCase(element, "usd_zwl", out JsonElement compactPair) && TryReadDecimal(compactPair, out decimal compactRate))
                return compactRate;

            foreach (JsonProperty property in element.EnumerateObject())
            {
                decimal? nested = TryExtractUsdZwlRate(property.Value);
                if (nested is > 0)
                    return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                decimal? nested = TryExtractUsdZwlRate(item);
                if (nested is > 0)
                    return nested;
            }
        }

        return null;
    }

    private static bool TryExtractFromPairObject(JsonElement element, out decimal rate)
    {
        rate = 0;

        if (!TryGetPropertyIgnoreCase(element, "base", out JsonElement baseElement)
            || !TryGetPropertyIgnoreCase(element, "quote", out JsonElement quoteElement))
            return false;

        string baseCurrency = baseElement.GetString() ?? string.Empty;
        string quoteCurrency = quoteElement.GetString() ?? string.Empty;
        if (!baseCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase)
            || !quoteCurrency.Equals("ZWL", StringComparison.OrdinalIgnoreCase))
            return false;

        foreach (string field in RateFieldCandidates)
        {
            if (TryGetPropertyIgnoreCase(element, field, out JsonElement value) && TryReadDecimal(value, out decimal parsed))
            {
                rate = parsed;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.NameEquals(propertyName) || property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryReadDecimal(JsonElement value, out decimal decimalValue)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Number:
                return value.TryGetDecimal(out decimalValue);
            case JsonValueKind.String:
                return decimal.TryParse(
                    value.GetString(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimalValue);
            default:
                decimalValue = 0;
                return false;
        }
    }
}
