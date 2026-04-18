using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Infrastructure.Caching;
using ZimMarket.Infrastructure.Configuration;
using ZimMarket.Infrastructure.Notifications;
using ZimMarket.Infrastructure.Payments;
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

        return services;
    }
}
