using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Logistics;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Infrastructure.BackgroundJobs;
using ZimMarket.Infrastructure.Caching;
using ZimMarket.Infrastructure.Configuration;
using ZimMarket.Infrastructure.Authentication;
using ZimMarket.Infrastructure.ExchangeRates;
using ZimMarket.Infrastructure.Identity;
using ZimMarket.Infrastructure.Notifications;
using ZimMarket.Infrastructure.Payments;
using ZimMarket.Infrastructure.Persistence;
using ZimMarket.Infrastructure.Persistence.Repositories;
using ZimMarket.Infrastructure.RealTime;
using ZimMarket.Infrastructure.Security;
using ZimMarket.Infrastructure.Storage;

namespace ZimMarket.Infrastructure;

public static class DependencyInjection
{
    private const int DbTransientRetryCount = 5;
    private static readonly TimeSpan DbTransientRetryMaxDelay = TimeSpan.FromSeconds(10);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSignalR();
        services.AddScoped<ITrackingHubSubscriptionService>(sp =>
        {
            IUnitOfWork? unitOfWork = sp.GetService<IUnitOfWork>();
            return unitOfWork is null
                ? new DisabledTrackingHubSubscriptionService()
                : new TrackingHubSubscriptionService(unitOfWork);
        });
        services.AddScoped<IDriverTrackingBroadcaster, DriverTrackingSignalRBroadcaster>();

        RegisterJwt(services, configuration);
        services.AddSingleton<IAuthTokenService, AuthTokenService>();
        services.AddSingleton<IAuthLinkBuilder, AuthLinkBuilder>();
        services.AddOptions<LogisticsOptions>()
            .Bind(configuration.GetSection(LogisticsOptions.SectionName));
        RegisterEntityFramework(services, configuration);
        RegisterRedis(services, configuration);
        RegisterFileStorage(services, configuration);
        RegisterPaymentGateways(services, configuration);
        RegisterExchangeRates(services, configuration);
        RegisterTwilio(services, configuration);
        RegisterEmail(services, configuration);
        RegisterFirebase(services, configuration);
        services.AddZimMarketHangfire(configuration);
        if (HangfireJobSetup.IsHangfireStorageConfigured(configuration))
            services.AddTransient<INotificationJobScheduler, HangfireNotificationJobScheduler>();

        return services;
    }

    private static void RegisterExchangeRates(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ExchangeRateProviderOptions>()
            .Bind(configuration.GetSection(ExchangeRateProviderOptions.SectionName))
            .Validate(
                options => options.FallbackUsdToZwlRate > 0,
                "ExchangeRate:FallbackUsdToZwlRate must be greater than zero.");

        services.AddHttpClient<IUsdZwlRateProvider, RbzUsdZwlRateProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });
    }

    private static void RegisterJwt(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                o => string.IsNullOrWhiteSpace(o.PrivateKeyPem) == string.IsNullOrWhiteSpace(o.PublicKeyPem),
                "Jwt:PrivateKeyPem and Jwt:PublicKeyPem must both be set or both be empty (empty disables token operations until configured).");

        services.AddSingleton<IJwtService, JwtService>();
    }

    private static void RegisterEntityFramework(IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:DefaultConnection"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddScoped<IUnitOfWork, UnavailableUnitOfWork>();
            services.AddScoped<IUserIdentityReadRepository, UnavailableUserIdentityReadRepository>();
            services.AddScoped<IUserLoginRepository, UnavailableUserLoginRepository>();
            services.AddScoped<IExchangeRateService, FallbackExchangeRateService>();
            services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
            return;
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: DbTransientRetryCount,
                        maxRetryDelay: DbTransientRetryMaxDelay,
                        errorCodesToAdd: null);
                }));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserIdentityReadRepository, UserIdentityReadRepository>();
        services.AddScoped<IUserLoginRepository, UserLoginRepository>();
        services.AddScoped<IExchangeRateService, ExchangeRateService>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        services.AddIdentityCore<IdentityUser<Guid>>(identityOptions =>
            {
                identityOptions.User.RequireUniqueEmail = true;
                identityOptions.Password.RequiredLength = 8;
                identityOptions.Password.RequireDigit = true;
                identityOptions.Password.RequireUppercase = true;
                identityOptions.Lockout.AllowedForNewUsers = false;
            })
            .AddUserStore<ZimMarketUserStore>();
    }

    private static void RegisterRedis(IServiceCollection services, IConfiguration configuration)
    {
        string? redisConnectionString =
            configuration["Redis:ConnectionString"]
            ?? configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(redisConnectionString))
            return;

        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.ConnectionString))
                    options.ConnectionString = redisConnectionString;
            })
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "Redis connection string is missing.");

        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(
                sp.GetRequiredService<IOptions<RedisOptions>>().Value.ConnectionString));

        services.AddSingleton<ICacheService, RedisCacheService>();
    }

    private static void RegisterFileStorage(IServiceCollection services, IConfiguration configuration)
    {
        string provider = configuration["Storage:Provider"]?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(provider))
        {
            string? azureBlobConnectionString =
                configuration["AzureBlob:ConnectionString"]
                ?? configuration.GetConnectionString("AzureBlob");

            provider = string.IsNullOrWhiteSpace(azureBlobConnectionString)
                ? "Unavailable"
                : "AzureBlob";
        }

        if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            RegisterLocalFileStorage(services, configuration);
            return;
        }

        if (provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            RegisterAzureBlobStorage(services, configuration);
            return;
        }

        services.AddSingleton<IFileStorage, UnavailableFileStorage>();
    }

    private static void RegisterLocalFileStorage(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LocalFileStorageOptions>()
            .Bind(configuration.GetSection(LocalFileStorageOptions.SectionName))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.PublicBaseUrl))
                {
                    options.PublicBaseUrl =
                        configuration["Storage:PublicBaseUrl"]
                        ?? configuration["Api:PublicBaseUrl"]
                        ?? "http://localhost:8080";
                }
            })
            .ValidateDataAnnotations()
            .Validate(
                o => Uri.TryCreate(o.PublicBaseUrl, UriKind.Absolute, out _),
                "LocalFileStorage:PublicBaseUrl must be an absolute URL.")
            .Validate(
                o => o.ReadUrlTtlKyc > TimeSpan.Zero && o.ReadUrlTtlDefault > TimeSpan.Zero && o.WriteUrlTtl > TimeSpan.Zero,
                "Local file storage read/write URL TTL values must be positive.")
            .ValidateOnStart();

        services.AddSingleton<LocalFileStorage>();
        services.AddSingleton<IFileStorage>(sp => sp.GetRequiredService<LocalFileStorage>());
        services.AddSingleton<ILocalFileStorageAccess>(sp => sp.GetRequiredService<LocalFileStorage>());
    }

    private static void RegisterAzureBlobStorage(IServiceCollection services, IConfiguration configuration)
    {
        string? azureBlobConnectionString =
            configuration["AzureBlob:ConnectionString"]
            ?? configuration.GetConnectionString("AzureBlob");

        if (string.IsNullOrWhiteSpace(azureBlobConnectionString))
        {
            services.AddSingleton<IFileStorage, UnavailableFileStorage>();
            return;
        }

        try
        {
            _ = new BlobServiceClient(azureBlobConnectionString);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            services.AddSingleton<IFileStorage, UnavailableFileStorage>();
            return;
        }

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

    private static void RegisterPaymentGateways(IServiceCollection services, IConfiguration configuration)
    {
        int paynowIntegrationId = configuration.GetValue<int>("Paynow:IntegrationId");
        string? paynowIntegrationKey = configuration["Paynow:IntegrationKey"];
        if (paynowIntegrationId > 0 && !string.IsNullOrWhiteSpace(paynowIntegrationKey))
        {
            services.AddOptions<PaynowOptions>()
                .Bind(configuration.GetSection(PaynowOptions.SectionName))
                .PostConfigure(options =>
                {
                    if (string.IsNullOrWhiteSpace(options.IntegrationKey))
                        options.IntegrationKey = paynowIntegrationKey;
                    if (options.IntegrationId <= 0)
                        options.IntegrationId = paynowIntegrationId;
                })
                .ValidateDataAnnotations()
                .Validate(
                    o => !string.IsNullOrWhiteSpace(o.ReturnUrlTemplate)
                        && o.ReturnUrlTemplate.Contains("{0}", StringComparison.Ordinal),
                    "Paynow:ReturnUrlTemplate must contain '{0}' for the order id.")
                .Validate(
                    o => !string.IsNullOrWhiteSpace(o.ResultUrlTemplate)
                        && o.ResultUrlTemplate.Contains("{0}", StringComparison.Ordinal),
                    "Paynow:ResultUrlTemplate must contain '{0}' for the order id.")
                .ValidateOnStart();

            services.AddHttpClient("Paynow", client => client.Timeout = TimeSpan.FromSeconds(60));

            services.AddSingleton<PaynowService>(sp => new PaynowService(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("Paynow"),
                sp.GetRequiredService<IOptions<PaynowOptions>>(),
                sp.GetRequiredService<ILogger<PaynowService>>(),
                sp.GetRequiredService<IHostEnvironment>()));

            services.AddKeyedSingleton<IPaymentGateway>(PaymentGatewayKeys.Paynow, (sp, _) => sp.GetRequiredService<PaynowService>());
        }

        services.AddSingleton<IPaymentGatewayFactory, PaymentGatewayFactory>();
    }

    private static void RegisterTwilio(IServiceCollection services, IConfiguration configuration)
    {
        string? twilioAccountSid = configuration["Twilio:AccountSid"];
        string? twilioAuthToken = configuration["Twilio:AuthToken"];
        string? twilioFrom = configuration["Twilio:FromPhoneNumber"];
        if (!string.IsNullOrWhiteSpace(twilioAccountSid)
            && !string.IsNullOrWhiteSpace(twilioAuthToken)
            && !string.IsNullOrWhiteSpace(twilioFrom))
        {
            services.AddOptions<TwilioOptions>()
                .Bind(configuration.GetSection(TwilioOptions.SectionName))
                .PostConfigure(options =>
                {
                    if (string.IsNullOrWhiteSpace(options.AccountSid))
                        options.AccountSid = twilioAccountSid;
                    if (string.IsNullOrWhiteSpace(options.AuthToken))
                        options.AuthToken = twilioAuthToken;
                    if (string.IsNullOrWhiteSpace(options.FromPhoneNumber))
                        options.FromPhoneNumber = twilioFrom;
                })
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<ISmsService, TwilioSmsService>();
        }
    }

    private static void RegisterEmail(IServiceCollection services, IConfiguration configuration)
    {
        string? smtpHost = configuration["Smtp:Host"] ?? configuration["SMTP_HOST"];
        string? smtpUser = configuration["Smtp:User"] ?? configuration["SMTP_USER"];
        string? smtpPassword = configuration["Smtp:Password"] ?? configuration["SMTP_PASSWORD"];
        string? smtpFrom = configuration["Smtp:FromEmail"] ?? configuration["EMAIL_FROM"] ?? configuration["SMTP_USER"];
        if (!string.IsNullOrWhiteSpace(smtpHost)
            && !string.IsNullOrWhiteSpace(smtpUser)
            && !string.IsNullOrWhiteSpace(smtpPassword)
            && !string.IsNullOrWhiteSpace(smtpFrom))
        {
            services.AddOptions<SmtpOptions>()
                .Bind(configuration.GetSection(SmtpOptions.SectionName))
                .PostConfigure(options =>
                {
                    if (string.IsNullOrWhiteSpace(options.Host))
                        options.Host = smtpHost;
                    if (string.IsNullOrWhiteSpace(options.User))
                        options.User = smtpUser;
                    if (string.IsNullOrWhiteSpace(options.Password))
                        options.Password = smtpPassword;
                    if (string.IsNullOrWhiteSpace(options.FromEmail))
                        options.FromEmail = smtpFrom;
                    if (options.Port <= 0)
                        options.Port = configuration.GetValue("SMTP_PORT", 587);
                    if (string.IsNullOrWhiteSpace(options.FromName))
                        options.FromName = configuration["EMAIL_FROM_NAME"] ?? "ZimMarket";
                    options.EnableSsl = configuration.GetValue("SMTP_ENABLE_SSL", true);
                })
                .ValidateDataAnnotations()
                .Validate(o => o.Port is > 0 and <= 65535, "Smtp:Port must be between 1 and 65535.")
                .ValidateOnStart();

            services.AddSingleton<IEmailService, SmtpEmailService>();
            return;
        }

        string? sendGridApiKey = configuration["SendGrid:ApiKey"];
        if (!string.IsNullOrWhiteSpace(sendGridApiKey))
        {
            services.AddOptions<SendGridOptions>()
                .Bind(configuration.GetSection(SendGridOptions.SectionName))
                .PostConfigure(options =>
                {
                    if (string.IsNullOrWhiteSpace(options.ApiKey))
                        options.ApiKey = sendGridApiKey;
                })
                .ValidateDataAnnotations()
                .Validate(
                    o => !string.IsNullOrWhiteSpace(o.FromEmail),
                    "SendGrid:FromEmail is required when SendGrid is enabled.")
                .ValidateOnStart();

            services.AddSingleton<IEmailService, SendGridEmailService>();
        }
    }

    private static void RegisterFirebase(IServiceCollection services, IConfiguration configuration)
    {
        bool firebaseAdc = configuration.GetValue("Firebase:UseApplicationDefaultCredentials", false);
        bool firebaseHasJson = !string.IsNullOrWhiteSpace(configuration["Firebase:CredentialsJson"]);
        bool firebaseHasPath = !string.IsNullOrWhiteSpace(configuration["Firebase:CredentialsPath"]);
        if (firebaseAdc || firebaseHasJson || firebaseHasPath)
        {
            services.AddOptions<FirebaseAdminOptions>()
                .Bind(configuration.GetSection(FirebaseAdminOptions.SectionName))
                .Validate(
                    o => o.UseApplicationDefaultCredentials
                        || !string.IsNullOrWhiteSpace(o.CredentialsJson)
                        || !string.IsNullOrWhiteSpace(o.CredentialsPath),
                    "Firebase: set UseApplicationDefaultCredentials, CredentialsJson, or CredentialsPath.")
                .ValidateOnStart();

            services.AddSingleton<IPushNotificationService, FcmPushNotificationService>();
        }
    }
}
