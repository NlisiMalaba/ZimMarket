using Microsoft.Extensions.Options;

namespace ZimMarket.API.Configuration;

public static class ZimMarketConfigurationValidationExtensions
{
    public static IServiceCollection AddZimMarketConfigurationValidation(
        this IServiceCollection services,
        IConfiguration configuration,
        bool enforceRequiredConfiguration = true)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (!enforceRequiredConfiguration)
            return services;

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

                options.StorageProvider = configuration["Storage:Provider"] ?? string.Empty;

                options.AzureBlobConnectionString =
                    configuration["AzureBlob:ConnectionString"]
                    ?? configuration.GetConnectionString("AzureBlob")
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(options.StorageProvider)
                    && !string.IsNullOrWhiteSpace(options.AzureBlobConnectionString))
                {
                    options.StorageProvider = "AzureBlob";
                }

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
                options =>
                    options.StorageProvider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase)
                    || options.StorageProvider.Equals("Local", StringComparison.OrdinalIgnoreCase),
                "Storage:Provider must be either 'AzureBlob' or 'Local'.")
            .Validate(
                options =>
                    !options.StorageProvider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase)
                    || !string.IsNullOrWhiteSpace(options.AzureBlobConnectionString),
                "AzureBlob:ConnectionString must be configured when Storage:Provider is 'AzureBlob'.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.JwtPrivateKeyPem) == !string.IsNullOrWhiteSpace(options.JwtPublicKeyPem),
                "Jwt:PrivateKeyPem and Jwt:PublicKeyPem must both be configured.")
            .ValidateOnStart();

        return services;
    }
}
