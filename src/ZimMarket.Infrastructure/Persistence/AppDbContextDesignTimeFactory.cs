using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ZimMarket.Infrastructure.Persistence;

/// <summary>
/// EF Core design-time factory. Resolves the connection string from environment variables (preferred for CI)
/// or from <c>ZimMarket.API/appsettings*.json</c> when present.
/// </summary>
public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        string connectionString = ResolveConnectionString();

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        IPublisher publisher = new DesignTimeNoOpPublisher();
        return new AppDbContext(options, publisher);
    }

    private static string ResolveConnectionString()
    {
        string? fromEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        string apiProjectDir = FindDirectoryContainingFile("ZimMarket.API.csproj");

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string? fromConfig =
            configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:DefaultConnection"];

        if (!string.IsNullOrWhiteSpace(fromConfig))
            return fromConfig;

        // Allows `dotnet ef migrations add` without a live database; override for `database update`.
        return "Host=127.0.0.1;Port=5432;Database=zimmarket_dev;Username=postgres;Password=postgres";
    }

    private static string FindDirectoryContainingFile(string fileName)
    {
        string? directory = Directory.GetCurrentDirectory();
        for (int depth = 0; depth < 12 && directory is not null; depth++)
        {
            if (File.Exists(Path.Combine(directory, fileName)))
                return directory;

            string nested = Path.Combine(directory, "src", "ZimMarket.API", fileName);
            if (File.Exists(nested))
                return Path.GetDirectoryName(nested)!;

            directory = Directory.GetParent(directory)?.FullName;
        }

        return Directory.GetCurrentDirectory();
    }
}
