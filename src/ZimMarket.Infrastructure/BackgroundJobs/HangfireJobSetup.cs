using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZimMarket.Infrastructure.BackgroundJobs.Jobs;

namespace ZimMarket.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire storage, server, recurring schedules, and dashboard wiring.
/// </summary>
public static class HangfireJobSetup
{
    public const string DashboardPath = "/hangfire";

    internal const string RecurringJobUpdateExchangeRate = "update-exchange-rate-usd-zwl";
    internal const string RecurringJobCleanExpiredTokens = "clean-expired-refresh-tokens";
    internal const string RecurringJobBatchStaleOrders = "batch-stale-unpaid-orders";
    internal const string RecurringJobArchiveDeliveryData = "archive-old-delivery-data";

    /// <summary>
    /// Resolves the same PostgreSQL connection used by EF Core migrations and the API.
    /// </summary>
    public static string? ResolvePostgreSqlConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:DefaultConnection"];
    }

    public static bool IsHangfireStorageConfigured(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(ResolvePostgreSqlConnectionString(configuration));

    public static IServiceCollection AddZimMarketHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string? connectionString = ResolvePostgreSqlConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
            return services;

        services.AddHangfire((_, hangfire) =>
            hangfire
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
                    configure => configure.UseNpgsqlConnection(connectionString),
                    new PostgreSqlStorageOptions
                    {
                        SchemaName = "hangfire",
                        PrepareSchemaIfNecessary = true,
                        EnableTransactionScopeEnlistment = true
                    }));

        services.AddHangfireServer();

        services.AddTransient<UpdateExchangeRateJob>();
        services.AddTransient<CleanExpiredTokensJob>();
        services.AddTransient<BatchStaleOrdersJob>();
        services.AddTransient<ArchiveOldDeliveryDataJob>();

        return services;
    }

    /// <summary>
    /// Registers or updates all recurring jobs (idempotent; safe on every startup).
    /// </summary>
    public static void RegisterRecurringJobs(IRecurringJobManager recurringJobs)
    {
        ArgumentNullException.ThrowIfNull(recurringJobs);

        recurringJobs.AddOrUpdate<UpdateExchangeRateJob>(
            RecurringJobUpdateExchangeRate,
            job => job.Execute(),
            Cron.Daily(hour: 6, minute: 0));

        recurringJobs.AddOrUpdate<CleanExpiredTokensJob>(
            RecurringJobCleanExpiredTokens,
            job => job.Execute(),
            Cron.Daily(hour: 3, minute: 0));

        recurringJobs.AddOrUpdate<BatchStaleOrdersJob>(
            RecurringJobBatchStaleOrders,
            job => job.Execute(),
            Cron.MinuteInterval(interval: 30));

        recurringJobs.AddOrUpdate<ArchiveOldDeliveryDataJob>(
            RecurringJobArchiveDeliveryData,
            job => job.Execute(),
            Cron.Weekly(dayOfWeek: DayOfWeek.Sunday, hour: 3));
    }

    public static IApplicationBuilder UseZimMarketHangfireDashboard(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseHangfireDashboard(
            DashboardPath,
            new DashboardOptions
            {
                Authorization = [new HangfireAuthFilter()],
                DisplayStorageConnectionString = false
            });
    }

    /// <summary>
    /// Registers recurring schedules when Hangfire storage is configured for this host.
    /// </summary>
    public static IHost RegisterZimMarketHangfireRecurringJobs(this IHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();
        if (!IsHangfireStorageConfigured(configuration))
            return host;

        IRecurringJobManager recurringJobs = host.Services.GetRequiredService<IRecurringJobManager>();
        RegisterRecurringJobs(recurringJobs);

        return host;
    }
}
