using FluentAssertions;
using ZimMarket.Integration.Tests.Fixtures;

namespace ZimMarket.Integration.Tests;

public sealed class RedisCacheServiceIntegrationTests : IClassFixture<RedisFixture>
{
    private readonly RedisFixture _fixture;

    public RedisCacheServiceIntegrationTests(RedisFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Set_get_roundtrip_then_ttl_expires_returns_null()
    {
        string prefix = $"{Guid.NewGuid():n}:";
        var cache = _fixture.CreateCacheService(prefix);
        const string key = "ttl-key";

        await cache.SetAsync(key, "payload-a", TimeSpan.FromSeconds(1));

        (await cache.GetAsync<string>(key)).Should().Be("payload-a");

        await Task.Delay(TimeSpan.FromSeconds(2.5));

        (await cache.GetAsync<string>(key)).Should().BeNull();
    }

    [Fact]
    public async Task RemoveByPattern_removes_only_matching_keys()
    {
        string prefix = $"{Guid.NewGuid():n}:";
        var cache = _fixture.CreateCacheService(prefix);

        await cache.SetAsync("pat:one", "v1", ttl: null);
        await cache.SetAsync("pat:two", "v2", ttl: null);
        await cache.SetAsync("other:keep", "keep", ttl: null);

        await cache.RemoveByPatternAsync("pat:*");

        (await cache.GetAsync<string>("pat:one")).Should().BeNull();
        (await cache.GetAsync<string>("pat:two")).Should().BeNull();
        (await cache.GetAsync<string>("other:keep")).Should().Be("keep");
    }
}
