namespace ZimMarket.API.Cors;

public static class ZimMarketCorsExtensions
{
    public const string PolicyName = "zimmarket-allowlist";

    public static IServiceCollection AddZimMarketCors(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string[] mobileOrigins = configuration.GetSection("Cors:MobileAppOrigins").Get<string[]>() ?? [];
        string? adminPanelOrigin = configuration["Cors:AdminPanelOrigin"];

        var origins = mobileOrigins
            .Append(adminPanelOrigin)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (origins.Length == 0)
            throw new InvalidOperationException("CORS configuration is missing. Provide Cors:MobileAppOrigins and/or Cors:AdminPanelOrigin.");

        foreach (string origin in origins)
        {
            if (origin == "*" || origin.Contains('*', StringComparison.Ordinal))
                throw new InvalidOperationException("CORS wildcard origins are not allowed. Configure explicit origins only.");

            if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri)
                || string.IsNullOrWhiteSpace(uri.Scheme)
                || string.IsNullOrWhiteSpace(uri.Host))
            {
                throw new InvalidOperationException($"Invalid CORS origin configured: '{origin}'. Use absolute origins like https://admin.zimmarket.com.");
            }
        }

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
