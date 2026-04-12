namespace ZimMarket.Application.Common.Interfaces;

public interface IFileStorage
{
    Task<string> UploadAsync(
        Stream stream,
        string key,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<string> GenerateSasUrlAsync(
        string key,
        DateTimeOffset expiry,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    Task<string> GetPresignedUploadUrlAsync(
        string key,
        string contentType,
        CancellationToken cancellationToken = default);
}
