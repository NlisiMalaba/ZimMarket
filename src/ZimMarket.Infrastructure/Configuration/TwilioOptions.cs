using System.ComponentModel.DataAnnotations;

namespace ZimMarket.Infrastructure.Configuration;

public sealed class TwilioOptions
{
    public const string SectionName = "Twilio";

    [Required]
    public string AccountSid { get; set; } = string.Empty;

    [Required]
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>Sender phone number in E.164 format (e.g. +263...).</summary>
    [Required]
    public string FromPhoneNumber { get; set; } = string.Empty;
}
