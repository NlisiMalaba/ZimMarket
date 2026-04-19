using Microsoft.Extensions.Options;

namespace ZimMarket.API.Configuration;

public static class ZimMarketConfigurationValidationExtensions
{
    public static IServiceCollection AddZimMarketConfigurationValidation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<ZimMarketRequiredConfigurationOptions>()
            .Configure(options =>
            {
                options.DefaultConnection =
                    configuration.GetConnectionString("DefaultConnection")
                    ?? configuration["ConnectionStrings:DefaultConnection"]
                    ?? string.Empty;

                options.RedisConnectionString =
                    configuration["Redis:ConnectionString"]
                    ?? configuration.GetConnectionString("Redis")
                    ?? string.Empty;

                options.AzureBlobConnectionString =
                    configuration["AzureBlob:ConnectionString"]
                    ?? configuration.GetConnectionString("AzureBlob")
                    ?? string.Empty;

                options.JwtIssuer = configuration["Jwt:Issuer"] ?? string.Empty;
                options.JwtAudience = configuration["Jwt:Audience"] ?? string.Empty;
                options.JwtPrivateKeyPem = configuration["Jwt:PrivateKeyPem"] ?? string.Empty;
                options.JwtPublicKeyPem = configuration["Jwt:PublicKeyPem"] ?? string.Empty;

                options.MobileAppOrigins = configuration.GetSection("Cors:MobileAppOrigins").Get<string[]>() ?? [];
                options.AdminPanelOrigin = configuration["Cors:AdminPanelOrigin"] ?? string.Empty;
            })
            .ValidateDataAnnotations()
            .Validate(
                options =>
                    !options.MobileAppOrigins.Any(origin =>
                        string.IsNullOrWhiteSpace(origin)
                        || origin.Contains('*', StringComparison.Ordinal)
                        || !Uri.TryCreate(origin, UriKind.Absolute, out _)),
                "Cors:MobileAppOrigins must contain explicit absolute origins only (wildcards are not allowed).")
            .Validate(
                options =>
                    !options.AdminPanelOrigin.Contains('*', StringComparison.Ordinal)
                    && Uri.TryCreate(options.AdminPanelOrigin, UriKind.Absolute, out _),
                "Cors:AdminPanelOrigin must be an explicit absolute origin (wildcards are not allowed).")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.JwtPrivateKeyPem) == !string.IsNullOrWhiteSpace(options.JwtPublicKeyPem),
                "Jwt:PrivateKeyPem and Jwt:PublicKeyPem must both be configured.")
            .ValidateOnStart();

        return services;
    }
}
