using System.ComponentModel.DataAnnotations;

namespace ZimMarket.Infrastructure.Configuration;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    [Required]
    [EmailAddress]
    public string User { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "ZimMarket";

    public bool EnableSsl { get; set; } = true;
}
