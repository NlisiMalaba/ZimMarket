using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Infrastructure.Configuration;

namespace ZimMarket.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage, ILocalFileStorageAccess
{
    public const string ContainerProductImages = "product-images";
    public const string ContainerKycDocuments = "kyc-documents";
    public const string ContainerDeliveryPhotos = "delivery-photos";

    private const string UploadPurpose = "upload";
    private const string ReadPurpose = "read";

    private static readonly HashSet<string> AllowedContainers =
    [
        ContainerProductImages,
        ContainerKycDocuments,
        ContainerDeliveryPhotos
    ];

    private static readonly Dictionary<string, string> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".pdf"] = "application/pdf"
    };

    private readonly LocalFileStorageOptions _options;
    private readonly ILogger<LocalFileStorage> _logger;
    private readonly string _rootPath;
    private readonly byte[] _signingKey;

    public LocalFileStorage(
        IOptions<LocalFileStorageOptions> options,
        IHostEnvironment environment,
        ILogger<LocalFileStorage> logger)
    {
        ArgumentNullException.ThrowIfNull(environment);

        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rootPath = ResolveRootPath(environment.ContentRootPath, _options.RootPath);
        _signingKey = ResolveSigningKey(_options.SigningKey);
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string key,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        (string containerName, string relativePath) = ParseAndValidateKey(key);
        EnsureContentTypeAllowed(containerName, contentType);

        string fullPath = GetSafeFullPath(containerName, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Stored local file {FileKey} at {FilePath}.", key, fullPath);
        return key;
    }

    public Task<string> GenerateSasUrlAsync(
        string key,
        DateTimeOffset expiry,
        CancellationToken cancellationToken = default)
    {
        (string containerName, _) = ParseAndValidateKey(key);
        DateTimeOffset effectiveExpiry = ClampReadExpiry(containerName, expiry);
        string signature = Sign(ReadPurpose, key, contentType: null, effectiveExpiry);
        string url = BuildLocalUrl("local-read", key, effectiveExpiry, signature, contentType: null);
        return Task.FromResult(url);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        (string containerName, string relativePath) = ParseAndValidateKey(key);
        string fullPath = GetSafeFullPath(containerName, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted local file {FileKey}.", key);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        (string containerName, string relativePath) = ParseAndValidateKey(key);
        string fullPath = GetSafeFullPath(containerName, relativePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<string> GetPresignedUploadUrlAsync(
        string key,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        (string containerName, _) = ParseAndValidateKey(key);
        string normalizedContentType = NormalizeContentType(contentType);
        EnsureContentTypeAllowed(containerName, normalizedContentType);

        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(_options.WriteUrlTtl);
        string signature = Sign(UploadPurpose, key, normalizedContentType, expiresAt);
        string url = BuildLocalUrl("local-upload", key, expiresAt, signature, normalizedContentType);
        return Task.FromResult(url);
    }

    public bool IsReadRequestAuthorized(string key, DateTimeOffset expiresAt, string signature)
    {
        if (expiresAt <= DateTimeOffset.UtcNow)
            return false;

        try
        {
            ParseAndValidateKey(key);
            string expected = Sign(ReadPurpose, key, contentType: null, expiresAt);
            return FixedTimeEquals(expected, signature);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public bool IsUploadRequestAuthorized(string key, string contentType, DateTimeOffset expiresAt, string signature)
    {
        if (expiresAt <= DateTimeOffset.UtcNow)
            return false;

        try
        {
            (string containerName, _) = ParseAndValidateKey(key);
            string normalizedContentType = NormalizeContentType(contentType);
            EnsureContentTypeAllowed(containerName, normalizedContentType);
            string expected = Sign(UploadPurpose, key, normalizedContentType, expiresAt);
            return FixedTimeEquals(expected, signature);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        (string containerName, string relativePath) = ParseAndValidateKey(key);
        string fullPath = GetSafeFullPath(containerName, relativePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Local file was not found.", key);

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult(stream);
    }

    public string GetContentType(string key)
    {
        string extension = Path.GetExtension(key);
        return ContentTypesByExtension.TryGetValue(extension, out string? contentType)
            ? contentType
            : "application/octet-stream";
    }

    private DateTimeOffset ClampReadExpiry(string containerName, DateTimeOffset requestedExpiry)
    {
        TimeSpan maxLifetime = containerName == ContainerKycDocuments
            ? _options.ReadUrlTtlKyc
            : _options.ReadUrlTtlDefault;

        DateTimeOffset policyCap = DateTimeOffset.UtcNow.Add(maxLifetime);
        DateTimeOffset effective = requestedExpiry < policyCap ? requestedExpiry : policyCap;

        if (effective <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedExpiry),
                "Effective read URL expiry must be in the future after applying configured TTL limits.");
        }

        return effective;
    }

    private string BuildLocalUrl(
        string endpoint,
        string key,
        DateTimeOffset expiresAt,
        string signature,
        string? contentType)
    {
        var query = new Dictionary<string, string?>
        {
            ["expires"] = expiresAt.ToUnixTimeSeconds().ToString(),
            ["signature"] = signature
        };

        if (!string.IsNullOrWhiteSpace(contentType))
            query["contentType"] = contentType;

        string baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        string routeKey = EscapeKeyForRoute(key);
        return QueryHelpers.AddQueryString($"{baseUrl}/api/v1/files/{endpoint}/{routeKey}", query);
    }

    private string Sign(string purpose, string key, string? contentType, DateTimeOffset expiresAt)
    {
        string payload = string.Join(
            '\n',
            purpose,
            key,
            contentType ?? string.Empty,
            expiresAt.ToUnixTimeSeconds().ToString());

        using var hmac = new HMACSHA256(_signingKey);
        byte[] signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return WebEncoders.Base64UrlEncode(signature);
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private string GetSafeFullPath(string containerName, string relativePath)
    {
        string combined = Path.GetFullPath(Path.Combine(_rootPath, containerName, relativePath));
        string rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;

        if (!combined.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File key resolves outside the configured storage root.");

        return combined;
    }

    private IReadOnlyList<string> GetAllowedContentTypes(string containerName) =>
        containerName switch
        {
            ContainerProductImages => _options.ProductImagesAllowedContentTypes,
            ContainerKycDocuments => _options.KycDocumentsAllowedContentTypes,
            ContainerDeliveryPhotos => _options.DeliveryPhotosAllowedContentTypes,
            _ => throw new ArgumentOutOfRangeException(nameof(containerName), containerName, null)
        };

    private void EnsureContentTypeAllowed(string containerName, string contentType)
    {
        string normalized = NormalizeContentType(contentType);
        IReadOnlyList<string> allowed = GetAllowedContentTypes(containerName);

        if (allowed.Any(t => string.Equals(t, normalized, StringComparison.OrdinalIgnoreCase)))
            return;

        throw new ArgumentException(
            $"Content type '{contentType}' is not allowed for container '{containerName}'.",
            nameof(contentType));
    }

    private static string NormalizeContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.", nameof(contentType));

        return contentType.Trim().ToLowerInvariant();
    }

    private static (string ContainerName, string RelativePath) ParseAndValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("File key is required.", nameof(key));

        string normalizedKey = key.Replace('\\', '/').Trim();
        int slash = normalizedKey.IndexOf('/');
        if (slash <= 0 || slash == normalizedKey.Length - 1)
        {
            throw new ArgumentException(
                $"Key must be '{{container}}/{{filePath}}' using one of: {string.Join(", ", AllowedContainers)}.",
                nameof(key));
        }

        string container = normalizedKey[..slash];
        string relativePath = normalizedKey[(slash + 1)..];

        if (!AllowedContainers.Contains(container))
        {
            throw new ArgumentException(
                $"Unknown container '{container}'. Expected one of: {string.Join(", ", AllowedContainers)}.",
                nameof(key));
        }

        if (relativePath.Split('/').Any(segment => segment is "" or "." or ".."))
            throw new ArgumentException("File key contains an invalid path segment.", nameof(key));

        return (container, relativePath);
    }

    private static string ResolveRootPath(string contentRootPath, string configuredRootPath)
    {
        string rootPath = string.IsNullOrWhiteSpace(configuredRootPath)
            ? "storage"
            : configuredRootPath;

        string resolved = Path.IsPathRooted(rootPath)
            ? rootPath
            : Path.Combine(contentRootPath, rootPath);

        return Path.GetFullPath(resolved);
    }

    private static byte[] ResolveSigningKey(string configuredSigningKey)
    {
        if (!string.IsNullOrWhiteSpace(configuredSigningKey))
            return SHA256.HashData(Encoding.UTF8.GetBytes(configuredSigningKey));

        return RandomNumberGenerator.GetBytes(32);
    }

    private static string EscapeKeyForRoute(string key) =>
        string.Join('/', key.Split('/').Select(Uri.EscapeDataString));
}
