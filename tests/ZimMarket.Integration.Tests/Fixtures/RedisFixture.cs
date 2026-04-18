using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.Redis;
using ZimMarket.Infrastructure.Caching;
using ZimMarket.Infrastructure.Configuration;

namespace ZimMarket.Integration.Tests.Fixtures;

public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7-alpine").Build();

    private IConnectionMultiplexer? _multiplexer;

    public async Task InitializeAsync() => await _container.StartAsync();

    public RedisCacheService CreateCacheService(string keyPrefix)
    {
        string connectionString = _container.GetConnectionString();
        _multiplexer ??= ConnectionMultiplexer.Connect(connectionString);

        IOptions<RedisOptions> options = Options.Create(new RedisOptions
        {
            ConnectionString = connectionString,
            KeyPrefix = keyPrefix
        });

        return new RedisCacheService(_multiplexer, options, NullLogger<RedisCacheService>.Instance);
    }

    public async Task DisposeAsync()
    {
        if (_multiplexer is not null)
        {
            await _multiplexer.CloseAsync();
            _multiplexer.Dispose();
            _multiplexer = null;
        }

        await _container.DisposeAsync();
    }
}
