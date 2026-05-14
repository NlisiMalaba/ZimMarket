using System.ComponentModel.DataAnnotations;

namespace ZimMarket.API.Configuration;

public sealed class ZimMarketRequiredConfigurationOptions
{
    [Required]
    public string DefaultConnection { get; set; } = string.Empty;

    [Required]
    public string RedisConnectionString { get; set; } = string.Empty;

    [Required]
    public string StorageProvider { get; set; } = string.Empty;

    public string AzureBlobConnectionString { get; set; } = string.Empty;

    [Required]
    public string JwtIssuer { get; set; } = string.Empty;

    [Required]
    public string JwtAudience { get; set; } = string.Empty;

    [Required]
    public string JwtPrivateKeyPem { get; set; } = string.Empty;

    [Required]
    public string JwtPublicKeyPem { get; set; } = string.Empty;

    [MinLength(1)]
    public string[] MobileAppOrigins { get; set; } = [];

    [Required]
    public string AdminPanelOrigin { get; set; } = string.Empty;
}
