using ZimMarket.Application.Common.Interfaces;

namespace ZimMarket.Infrastructure.Storage;

/// <summary>
/// Fallback file storage used when Azure Blob storage is not configured.
/// </summary>
public sealed class UnavailableFileStorage : IFileStorage
{
    private static InvalidOperationException CreateUnavailableException() =>
        new("File storage is not configured. Set AzureBlob:ConnectionString to enable file operations.");

    public Task<string> UploadAsync(
        Stream stream,
        string key,
        string contentType,
        CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();

    public Task<string> GenerateSasUrlAsync(
        string key,
        DateTimeOffset expiry,
        CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();

    public Task<string> GetPresignedUploadUrlAsync(
        string key,
        string contentType,
        CancellationToken cancellationToken = default) =>
        throw CreateUnavailableException();
}
