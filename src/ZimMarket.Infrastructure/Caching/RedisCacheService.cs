using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Infrastructure.Configuration;

namespace ZimMarket.Infrastructure.Caching;

/// <summary>
/// Redis-backed cache using <see cref="System.Text.Json"/> and <see cref="IConnectionMultiplexer"/>.
/// Pattern removal uses <c>SCAN</c> via StackExchange.Redis <see cref="IServer.KeysAsync"/> (not <c>KEYS</c> on Redis 2.8+).
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private const int ScanPageSize = 250;
    private const int DeleteBatchSize = 128;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IDatabase _database;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IConnectionMultiplexer multiplexer,
        IOptions<RedisOptions> options,
        ILogger<RedisCacheService> logger)
    {
        _multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = _multiplexer.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return default;

        try
        {
            RedisValue value = await _database.StringGetAsync(PrefixedKey(key)).ConfigureAwait(false);
            if (value.IsNullOrEmpty)
                return default;

            string json = value.ToString()!;
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis cache get failed for key {CacheKey}.", key);
            return default;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Redis cache deserialize failed for key {CacheKey}.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            string json = JsonSerializer.Serialize(value, JsonOptions);
            RedisKey redisKey = PrefixedKey(key);
            if (ttl.HasValue && ttl.Value > TimeSpan.Zero)
                await _database.StringSetAsync(redisKey, json, ttl.Value).ConfigureAwait(false);
            else
                await _database.StringSetAsync(redisKey, json).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis cache set failed for key {CacheKey}.", key);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Redis cache serialize failed for key {CacheKey}.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            await _database.KeyDeleteAsync(PrefixedKey(key)).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis cache remove failed for key {CacheKey}.", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return;

        RedisValue matchPattern = PrefixedPattern(pattern);
        int database = _database.Database;

        try
        {
            foreach (EndPoint endpoint in _multiplexer.GetEndPoints())
            {
                cancellationToken.ThrowIfCancellationRequested();

                IServer server = _multiplexer.GetServer(endpoint);
                if (!server.IsConnected || server.IsReplica)
                    continue;

                var batch = new List<RedisKey>(DeleteBatchSize);

                async Task FlushBatchAsync()
                {
                    if (batch.Count == 0)
                        return;

                    try
                    {
                        await _database.KeyDeleteAsync(batch.ToArray()).ConfigureAwait(false);
                    }
                    catch (RedisException ex)
                    {
                        _logger.LogWarning(ex, "Redis cache batch delete failed during pattern removal.");
                    }
                    finally
                    {
                        batch.Clear();
                    }
                }

                // StackExchange.Redis uses SCAN (not KEYS) on Redis 2.8+ for this enumerator.
                await foreach (RedisKey key in server
                                    .KeysAsync(database, matchPattern, pageSize: ScanPageSize)
                                    .ConfigureAwait(false)
                                    .WithCancellation(cancellationToken))
                {
                    batch.Add(key);
                    if (batch.Count >= DeleteBatchSize)
                        await FlushBatchAsync().ConfigureAwait(false);
                }

                await FlushBatchAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis cache remove-by-pattern failed for pattern {Pattern}.", pattern);
        }
    }

    private string Prefix => string.IsNullOrEmpty(_options.KeyPrefix) ? string.Empty : _options.KeyPrefix;

    private RedisKey PrefixedKey(string key) => $"{Prefix}{key}";

    private RedisValue PrefixedPattern(string pattern) => $"{Prefix}{pattern}";
}
