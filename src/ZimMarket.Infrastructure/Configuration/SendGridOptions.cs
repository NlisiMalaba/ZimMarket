using System.ComponentModel.DataAnnotations;

namespace ZimMarket.Infrastructure.Configuration;

public sealed class SendGridOptions
{
    public const string SectionName = "SendGrid";

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "ZimMarket";
}
