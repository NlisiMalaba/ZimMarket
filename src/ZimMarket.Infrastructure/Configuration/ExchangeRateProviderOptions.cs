namespace ZimMarket.Infrastructure.Configuration;

public sealed class ExchangeRateProviderOptions
{
    public const string SectionName = "ExchangeRate";

    /// <summary>
    /// Primary provider endpoint (RBZ or compatible JSON feed).
    /// </summary>
    public string PrimaryUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional fallback provider endpoint used when the primary request fails.
    /// </summary>
    public string FallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Fallback static value used if remote providers are unavailable.
    /// </summary>
    public decimal FallbackUsdToZwlRate { get; set; } = 26m;
}
