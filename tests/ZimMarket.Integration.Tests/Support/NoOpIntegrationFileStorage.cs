using ZimMarket.Application.Common.Interfaces;

namespace ZimMarket.Integration.Tests.Support;

/// <summary>Minimal <see cref="IFileStorage"/> for API integration tests when Azure Blob is not configured.</summary>
public sealed class NoOpIntegrationFileStorage : IFileStorage
{
    public Task<string> UploadAsync(
        Stream stream,
        string key,
        string contentType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(key);

    public Task<string> GenerateSasUrlAsync(
        string key,
        DateTimeOffset expiry,
        CancellationToken cancellationToken = default) =>
        Task.FromResult($"https://test.invalid/{key}");

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<string> GetPresignedUploadUrlAsync(
        string key,
        string contentType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult($"https://test.invalid/upload/{key}");
}
