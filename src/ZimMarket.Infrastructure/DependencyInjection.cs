using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Infrastructure.Caching;
using ZimMarket.Infrastructure.Configuration;
using ZimMarket.Infrastructure.Security;

namespace ZimMarket.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? redisConnectionString =
            configuration["Redis:ConnectionString"]
            ?? configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddOptions<RedisOptions>()
                .Bind(configuration.GetSection(RedisOptions.SectionName))
                .PostConfigure(options =>
                {
                    if (string.IsNullOrWhiteSpace(options.ConnectionString))
                        options.ConnectionString = redisConnectionString;
                })
                .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "Redis connection string is missing.");

            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(sp.GetRequiredService<IOptions<RedisOptions>>().Value.ConnectionString));

            services.AddSingleton<ICacheService, RedisCacheService>();
        }

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                o => string.IsNullOrWhiteSpace(o.PrivateKeyPem) == string.IsNullOrWhiteSpace(o.PublicKeyPem),
                "Jwt:PrivateKeyPem and Jwt:PublicKeyPem must both be set or both be empty (empty disables token operations until configured).");

        services.AddSingleton<IJwtService, JwtService>();

        return services;
    }
}
