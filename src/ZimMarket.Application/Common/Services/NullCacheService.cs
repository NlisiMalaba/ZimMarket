using ZimMarket.Application.Common.Interfaces;

namespace ZimMarket.Application.Common.Services;

/// <summary>
/// Cache that never stores values. Register a real <see cref="ICacheService"/> in Infrastructure to enable caching.
/// </summary>
public sealed class NullCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult<T?>(default);

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
