using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ZimMarket.API.Health;

public static class ZimMarketHealthChecksExtensions
{
    public const string LiveTag = "live";
    public const string ReadyTag = "ready";

    public static IServiceCollection AddZimMarketHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IHealthChecksBuilder builder = services.AddHealthChecks();

        builder.AddCheck("self", () => HealthCheckResult.Healthy(), tags: [LiveTag]);

        string? postgresConnection =
            configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:DefaultConnection"];

        if (!string.IsNullOrWhiteSpace(postgresConnection))
        {
            builder.AddNpgSql(
                postgresConnection,
                name: "postgres",
                tags: [ReadyTag]);
        }
        else
        {
            builder.AddCheck(
                "postgres",
                () => HealthCheckResult.Unhealthy("PostgreSQL connection string is not configured."),
                tags: [ReadyTag]);
        }

        string? redisConnection =
            configuration["Redis:ConnectionString"]
            ?? configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            builder.AddRedis(
                redisConnection,
                name: "redis",
                tags: [ReadyTag]);
        }
        else
        {
            builder.AddCheck(
                "redis",
                () => HealthCheckResult.Unhealthy("Redis connection string is not configured."),
                tags: [ReadyTag]);
        }

        return services;
    }

    public static WebApplication MapZimMarketHealthChecks(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks(
            "/health",
            new HealthCheckOptions { Predicate = registration => registration.Tags.Contains(LiveTag) });

        app.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions { Predicate = registration => registration.Tags.Contains(ReadyTag) });

        return app;
    }
}
