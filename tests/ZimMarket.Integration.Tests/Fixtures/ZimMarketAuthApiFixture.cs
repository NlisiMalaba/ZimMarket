using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Infrastructure.Persistence;
using ZimMarket.Integration.Tests.Support;

namespace ZimMarket.Integration.Tests.Fixtures;

/// <summary>Postgres (Testcontainers) + <see cref="WebApplicationFactory{TEntryPoint}"/> for auth HTTP tests.</summary>
public sealed class ZimMarketAuthApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    private WebApplicationFactory<Program>? _factory;
    private string? _priorSuperAdminPassword;
    private string? _priorConnectionString;
    private string? _priorJwtPrivate;
    private string? _priorJwtPublic;
    private string? _priorJwtIssuer;
    private string? _priorJwtAudience;
    private string? _priorJwtAccessSeconds;
    private string? _priorJwtRefreshIterations;
    private string? _priorTestRefreshBypass;
    private string? _priorRedisConnectionString;
    private string? _priorAzureBlobConnectionString;
    private string? _priorPaynowIntegrationId;
    private string? _priorPaynowIntegrationKey;

    public HttpClient CreateClient()
    {
        if (_factory is null)
            throw new InvalidOperationException("Fixture not initialized.");

        return _factory.CreateClient();
    }

    public IServiceScope CreateScope()
    {
        if (_factory is null)
            throw new InvalidOperationException("Fixture not initialized.");

        return _factory.Services.CreateScope();
    }

    public async Task InitializeAsync()
    {
        _priorSuperAdminPassword = Environment.GetEnvironmentVariable("ZIMMARKET_SUPERADMIN_PASSWORD");
        Environment.SetEnvironmentVariable("ZIMMARKET_SUPERADMIN_PASSWORD", "IntegrationTestPwd1!");

        await _postgres.StartAsync();
        await _redis.StartAsync();
        string connectionString = _postgres.GetConnectionString();

        await RunMigrationsAsync(connectionString);

        using RSA rsa = RSA.Create(2048);
        string privatePem = rsa.ExportPkcs8PrivateKeyPem();
        string publicPem = rsa.ExportSubjectPublicKeyInfoPem();

        StashAndSet("ConnectionStrings__DefaultConnection", connectionString);
        StashAndSet("Jwt__PrivateKeyPem", privatePem);
        StashAndSet("Jwt__PublicKeyPem", publicPem);
        StashAndSet("Jwt__Issuer", "ZimMarket-Integration");
        StashAndSet("Jwt__Audience", "ZimMarket-Integration");
        StashAndSet("Jwt__AccessTokenLifetimeSeconds", "120");
        StashAndSet("Jwt__RefreshTokenPbkdf2Iterations", "50000");
        StashAndSet("ZIMMARKET_TEST_ALLOW_REFRESH_WHILE_ACCESS_VALID", "1");
        StashAndSet("Redis__ConnectionString", _redis.GetConnectionString());
        StashAndSet("AzureBlob__ConnectionString", "UseDevelopmentStorage=true");
        StashAndSet("Paynow__IntegrationId", "12345");
        StashAndSet("Paynow__IntegrationKey", "integration-test-paynow-key");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IFileStorage>();
                services.AddSingleton<IFileStorage, NoOpIntegrationFileStorage>();
            });
        });
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
            _factory = null;
        }

        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();

        Environment.SetEnvironmentVariable("ZIMMARKET_SUPERADMIN_PASSWORD", _priorSuperAdminPassword);
        RestoreEnv("ConnectionStrings__DefaultConnection", _priorConnectionString);
        RestoreEnv("Jwt__PrivateKeyPem", _priorJwtPrivate);
        RestoreEnv("Jwt__PublicKeyPem", _priorJwtPublic);
        RestoreEnv("Jwt__Issuer", _priorJwtIssuer);
        RestoreEnv("Jwt__Audience", _priorJwtAudience);
        RestoreEnv("Jwt__AccessTokenLifetimeSeconds", _priorJwtAccessSeconds);
        RestoreEnv("Jwt__RefreshTokenPbkdf2Iterations", _priorJwtRefreshIterations);
        RestoreEnv("ZIMMARKET_TEST_ALLOW_REFRESH_WHILE_ACCESS_VALID", _priorTestRefreshBypass);
        RestoreEnv("Redis__ConnectionString", _priorRedisConnectionString);
        RestoreEnv("AzureBlob__ConnectionString", _priorAzureBlobConnectionString);
        RestoreEnv("Paynow__IntegrationId", _priorPaynowIntegrationId);
        RestoreEnv("Paynow__IntegrationKey", _priorPaynowIntegrationKey);
    }

    private void StashAndSet(string name, string value)
    {
        switch (name)
        {
            case "ConnectionStrings__DefaultConnection":
                _priorConnectionString = Environment.GetEnvironmentVariable(name);
                break;
            case "Jwt__PrivateKeyPem":
                _priorJwtPrivate = Environment.GetEnvironmentVariable(name);
                break;
            case "Jwt__PublicKeyPem":
                _priorJwtPublic = Environment.GetEnvironmentVariable(name);
                break;
            case "Jwt__Issuer":
                _priorJwtIssuer = Environment.GetEnvironmentVariable(name);
                break;
            case "Jwt__Audience":
                _priorJwtAudience = Environment.GetEnvironmentVariable(name);
                break;
            case "Jwt__AccessTokenLifetimeSeconds":
                _priorJwtAccessSeconds = Environment.GetEnvironmentVariable(name);
                break;
            case "Jwt__RefreshTokenPbkdf2Iterations":
                _priorJwtRefreshIterations = Environment.GetEnvironmentVariable(name);
                break;
            case "ZIMMARKET_TEST_ALLOW_REFRESH_WHILE_ACCESS_VALID":
                _priorTestRefreshBypass = Environment.GetEnvironmentVariable(name);
                break;
            case "Redis__ConnectionString":
                _priorRedisConnectionString = Environment.GetEnvironmentVariable(name);
                break;
            case "AzureBlob__ConnectionString":
                _priorAzureBlobConnectionString = Environment.GetEnvironmentVariable(name);
                break;
            case "Paynow__IntegrationId":
                _priorPaynowIntegrationId = Environment.GetEnvironmentVariable(name);
                break;
            case "Paynow__IntegrationKey":
                _priorPaynowIntegrationKey = Environment.GetEnvironmentVariable(name);
                break;
        }

        Environment.SetEnvironmentVariable(name, value);
    }

    private static void RestoreEnv(string name, string? value)
    {
        if (value is null)
            Environment.SetEnvironmentVariable(name, null);
        else
            Environment.SetEnvironmentVariable(name, value);
    }

    private static async Task RunMigrationsAsync(string connectionString)
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var db = new AppDbContext(options, new NoOpPublisher());
        await db.Database.MigrateAsync();
    }
}
