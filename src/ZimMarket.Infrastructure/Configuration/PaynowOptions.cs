using System.ComponentModel.DataAnnotations;

namespace ZimMarket.Infrastructure.Configuration;

public sealed class PaynowOptions
{
    public const string SectionName = "Paynow";

    [Range(1, int.MaxValue)]
    public int IntegrationId { get; set; }

    [Required]
    public string IntegrationKey { get; set; } = string.Empty;

    /// <summary>Full URL for the initiate-transaction POST (production default).</summary>
    [Required]
    [Url]
    public string InitiateTransactionUrl { get; set; } = "https://www.paynow.co.zw/interface/initiatetransaction";

    /// <summary>Customer return URL after checkout; must include <c>{0}</c> for <see cref="Guid"/> order id.</summary>
    [Required]
    public string ReturnUrlTemplate { get; set; } = string.Empty;

    /// <summary>Server URL for Paynow result webhooks; must include <c>{0}</c> for order id.</summary>
    [Required]
    public string ResultUrlTemplate { get; set; } = string.Empty;
}
