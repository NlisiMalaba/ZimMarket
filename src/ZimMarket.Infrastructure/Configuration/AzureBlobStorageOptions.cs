using System.ComponentModel.DataAnnotations;

namespace ZimMarket.Infrastructure.Configuration;

public sealed class AzureBlobStorageOptions
{
    public const string SectionName = "AzureBlob";

    /// <summary>Azure Storage connection string (account key credential).</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Maximum read SAS lifetime for blobs in <c>kyc-documents</c>.</summary>
    public TimeSpan ReadSasTtlKyc { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Maximum read SAS lifetime for <c>product-images</c> and <c>delivery-photos</c>.</summary>
    public TimeSpan ReadSasTtlDefault { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Lifetime for write (upload) SAS URLs.</summary>
    public TimeSpan WriteSasTtl { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Content types allowed for uploads to <c>product-images</c>.</summary>
    [MinLength(1)]
    public List<string> ProductImagesAllowedContentTypes { get; set; } =
        ["image/jpeg", "image/png", "image/webp"];

    /// <summary>Content types allowed for uploads to <c>kyc-documents</c>.</summary>
    [MinLength(1)]
    public List<string> KycDocumentsAllowedContentTypes { get; set; } =
        ["application/pdf", "image/jpeg", "image/png"];

    /// <summary>Content types allowed for uploads to <c>delivery-photos</c>.</summary>
    [MinLength(1)]
    public List<string> DeliveryPhotosAllowedContentTypes { get; set; } =
        ["image/jpeg", "image/png", "image/webp"];
}
