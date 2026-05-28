using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Infrastructure.Configuration;

namespace ZimMarket.Infrastructure.Storage;

/// <summary>
/// Azure Blob implementation of <see cref="IFileStorage"/>.
/// Keys must be <c>{container}/{blobPath}</c> where <c>container</c> is one of:
/// <c>product-images</c>, <c>profile-photos</c>, <c>kyc-documents</c>, or <c>delivery-photos</c>.
/// </summary>
public sealed class AzureBlobStorageService : IFileStorage
{
    public const string ContainerProductImages = "product-images";
    public const string ContainerProfilePhotos = "profile-photos";
    public const string ContainerKycDocuments = "kyc-documents";
    public const string ContainerDeliveryPhotos = "delivery-photos";

    private static readonly HashSet<string> AllowedContainers =
    [
        ContainerProductImages,
        ContainerProfilePhotos,
        ContainerKycDocuments,
        ContainerDeliveryPhotos
    ];

    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureBlobStorageOptions _options;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<AzureBlobStorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(
        Stream stream,
        string key,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        (string containerName, string blobPath) = ParseAndValidateKey(key);
        EnsureContentTypeAllowed(containerName, contentType);

        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        BlobClient blobClient = containerClient.GetBlobClient(blobPath);
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(stream, uploadOptions, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Uploaded blob {BlobKey}.", key);
        return key;
    }

    /// <inheritdoc />
    public Task<string> GenerateSasUrlAsync(
        string key,
        DateTimeOffset expiry,
        CancellationToken cancellationToken = default)
    {
        (string containerName, string blobPath) = ParseAndValidateKey(key);
        DateTimeOffset effectiveExpiry = ClampReadSasExpiry(containerName, expiry);

        BlobClient blobClient = _blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobPath);

        if (!blobClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException(
                "Cannot generate SAS URI: credential must be shared key (connection string with account key).");
        }

        Uri sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, effectiveExpiry);
        return Task.FromResult(sasUri.ToString());
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        (string containerName, string blobPath) = ParseAndValidateKey(key);
        BlobClient blobClient = _blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobPath);

        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        _logger.LogInformation("Deleted blob if existed {BlobKey}.", key);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        (string containerName, string blobPath) = ParseAndValidateKey(key);
        BlobClient blobClient = _blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobPath);

        Azure.Response<bool> exists = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
        return exists.Value;
    }

    /// <inheritdoc />
    public async Task<string> GetPresignedUploadUrlAsync(
        string key,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        (string containerName, string blobPath) = ParseAndValidateKey(key);
        EnsureContentTypeAllowed(containerName, contentType);

        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        BlobClient blobClient = containerClient.GetBlobClient(blobPath);

        if (!blobClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException(
                "Cannot generate SAS URI: credential must be shared key (connection string with account key).");
        }

        DateTimeOffset writeExpiry = DateTimeOffset.UtcNow.Add(_options.WriteSasTtl);
        const BlobSasPermissions uploadPermissions = BlobSasPermissions.Create | BlobSasPermissions.Write;
        Uri sasUri = blobClient.GenerateSasUri(uploadPermissions, writeExpiry);
        return sasUri.ToString();
    }

    private DateTimeOffset ClampReadSasExpiry(string containerName, DateTimeOffset requestedExpiry)
    {
        TimeSpan maxLifetime = containerName == ContainerKycDocuments
            ? _options.ReadSasTtlKyc
            : _options.ReadSasTtlDefault;

        DateTimeOffset policyCap = DateTimeOffset.UtcNow.Add(maxLifetime);
        DateTimeOffset effective = requestedExpiry < policyCap ? requestedExpiry : policyCap;

        if (effective <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedExpiry),
                "Effective read SAS expiry must be in the future after applying configured TTL limits.");
        }

        return effective;
    }

    private void EnsureContentTypeAllowed(string containerName, string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.", nameof(contentType));

        IReadOnlyList<string> allowed = GetAllowedContentTypes(containerName);
        if (allowed.Any(t => string.Equals(t, contentType.Trim(), StringComparison.OrdinalIgnoreCase)))
            return;

        throw new ArgumentException(
            $"Content type '{contentType}' is not allowed for container '{containerName}'.",
            nameof(contentType));
    }

    private IReadOnlyList<string> GetAllowedContentTypes(string containerName) =>
        containerName switch
        {
            ContainerProductImages => _options.ProductImagesAllowedContentTypes,
            ContainerProfilePhotos => _options.ProductImagesAllowedContentTypes,
            ContainerKycDocuments => _options.KycDocumentsAllowedContentTypes,
            ContainerDeliveryPhotos => _options.DeliveryPhotosAllowedContentTypes,
            _ => throw new ArgumentOutOfRangeException(nameof(containerName), containerName, null)
        };

    private static (string ContainerName, string BlobPath) ParseAndValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Blob key is required.", nameof(key));

        int slash = key.IndexOf('/');
        if (slash <= 0 || slash == key.Length - 1)
        {
            throw new ArgumentException(
                $"Key must be '{{container}}/{{blobPath}}' using one of: {string.Join(", ", AllowedContainers)}.",
                nameof(key));
        }

        string container = key[..slash];
        string blobPath = key[(slash + 1)..];

        if (!AllowedContainers.Contains(container))
        {
            throw new ArgumentException(
                $"Unknown container '{container}'. Expected one of: {string.Join(", ", AllowedContainers)}.",
                nameof(key));
        }

        return (container, blobPath);
    }
}
