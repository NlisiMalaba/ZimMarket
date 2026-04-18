using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Infrastructure.Caching;
using ZimMarket.Infrastructure.Configuration;
using ZimMarket.Infrastructure.Security;
using ZimMarket.Infrastructure.Storage;

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

        string? azureBlobConnectionString =
            configuration["AzureBlob:ConnectionString"]
            ?? configuration.GetConnectionString("AzureBlob");

        if (!string.IsNullOrWhiteSpace(azureBlobConnectionString))
        {
            services.AddOptions<AzureBlobStorageOptions>()
                .Bind(configuration.GetSection(AzureBlobStorageOptions.SectionName))
                .PostConfigure(options =>
                {
                    if (string.IsNullOrWhiteSpace(options.ConnectionString))
                        options.ConnectionString = azureBlobConnectionString;
                })
                .ValidateDataAnnotations()
                .Validate(
                    o => o.ReadSasTtlKyc > TimeSpan.Zero && o.ReadSasTtlDefault > TimeSpan.Zero && o.WriteSasTtl > TimeSpan.Zero,
                    "AzureBlob read/write SAS TTL values must be positive.")
                .ValidateOnStart();

            services.AddSingleton<BlobServiceClient>(sp =>
            {
                AzureBlobStorageOptions options = sp.GetRequiredService<IOptions<AzureBlobStorageOptions>>().Value;
                return new BlobServiceClient(options.ConnectionString);
            });

            services.AddSingleton<IFileStorage, AzureBlobStorageService>();
        }

        return services;
    }
}
