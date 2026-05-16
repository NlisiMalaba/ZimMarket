using System.ComponentModel.DataAnnotations;

namespace ZimMarket.Infrastructure.Configuration;

public sealed class LocalFileStorageOptions
{
    public const string SectionName = "LocalFileStorage";

    public string RootPath { get; set; } = "storage";

    public string PublicBaseUrl { get; set; } = "http://localhost:8080";

    public string SigningKey { get; set; } = string.Empty;

    public TimeSpan ReadUrlTtlKyc { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan ReadUrlTtlDefault { get; set; } = TimeSpan.FromHours(24);

    public TimeSpan WriteUrlTtl { get; set; } = TimeSpan.FromHours(1);

    [MinLength(1)]
    public List<string> ProductImagesAllowedContentTypes { get; set; } =
        ["image/jpeg", "image/png", "image/webp"];

    [MinLength(1)]
    public List<string> KycDocumentsAllowedContentTypes { get; set; } =
        ["application/pdf", "image/jpeg", "image/png"];

    [MinLength(1)]
    public List<string> DeliveryPhotosAllowedContentTypes { get; set; } =
        ["image/jpeg", "image/png", "image/webp"];
}
