using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Infrastructure.Persistence;

namespace ZimMarket.Integration.Tests.Fixtures;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    private readonly NoOpPublisher _publisher = new();
    private ServiceProvider? _serviceProvider;
    private string? _priorSuperAdminPassword;

    public IServiceProvider Services =>
        _serviceProvider ?? throw new InvalidOperationException("Fixture not initialized.");

    public async Task InitializeAsync()
    {
        // SeedDefaultData migration hashes this once; value is never persisted as plain text.
        _priorSuperAdminPassword = Environment.GetEnvironmentVariable("ZIMMARKET_SUPERADMIN_PASSWORD");
        Environment.SetEnvironmentVariable("ZIMMARKET_SUPERADMIN_PASSWORD", "IntegrationTestPwd1!");

        await _container.StartAsync();

        string connectionString = _container.GetConnectionString();

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using (var migrateContext = new AppDbContext(options, _publisher))
        {
            await migrateContext.Database.MigrateAsync();
        }

        var services = new ServiceCollection();
        services.AddSingleton<IPublisher>(_publisher);
        services.AddDbContext<AppDbContext>(builder => builder.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
            _serviceProvider = null;
        }

        await _container.DisposeAsync();

        Environment.SetEnvironmentVariable("ZIMMARKET_SUPERADMIN_PASSWORD", _priorSuperAdminPassword);
    }
}
