using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ZimMarket.Infrastructure.Persistence;

/// <summary>
/// EF Core design-time factory. Resolves the connection string from environment variables (preferred for CI),
/// repository root <c>.env</c> (same file as Docker Compose; does not override variables already set in the shell),
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
        RepositoryDotEnv.TryApply();

        string? fromEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return NormalizeDesignTimePostgresConnection(fromEnv);

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
            return NormalizeDesignTimePostgresConnection(fromConfig);

        string? fromDbParts = BuildConnectionStringFromDockerStyleEnv();
        if (!string.IsNullOrWhiteSpace(fromDbParts))
            return fromDbParts;

        // Allows `dotnet ef migrations add` without a live database; override for `database update`.
        return "Host=127.0.0.1;Port=5432;Database=zimmarket_dev;Username=postgres;Password=postgres";
    }

    /// <summary>
    /// When <c>.env</c> is written for containers, the host is often <c>postgres</c> and the published port is only on the machine.
    /// For design-time tools running on the host, point at localhost and <c>POSTGRES_HOST_PORT</c> when set.
    /// </summary>
    private static string NormalizeDesignTimePostgresConnection(string connectionString)
    {
        if (!connectionString.Contains("Host=postgres", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        string? hostPort = Environment.GetEnvironmentVariable("POSTGRES_HOST_PORT");
        if (string.IsNullOrWhiteSpace(hostPort))
            hostPort = "5432";

        string rewritten = Regex.Replace(
            connectionString,
            @"Host\s*=\s*postgres",
            "Host=127.0.0.1",
            RegexOptions.IgnoreCase);

        if (Regex.IsMatch(rewritten, @"Port\s*=", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)))
        {
            rewritten = Regex.Replace(
                rewritten,
                @"Port\s*=\s*\d+",
                $"Port={hostPort}",
                RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(1));
        }
        else
        {
            string trimmed = rewritten.TrimEnd().TrimEnd(';');
            rewritten = $"{trimmed};Port={hostPort}";
        }

        return rewritten;
    }

    private static string? BuildConnectionStringFromDockerStyleEnv()
    {
        string? db = Environment.GetEnvironmentVariable("DB_NAME");
        string? user = Environment.GetEnvironmentVariable("DB_USER");
        string? password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        if (string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            return null;

        string? hostPort = Environment.GetEnvironmentVariable("POSTGRES_HOST_PORT");
        if (string.IsNullOrWhiteSpace(hostPort))
            hostPort = "5432";

        return $"Host=127.0.0.1;Port={hostPort};Database={db};Username={user};Password={password}";
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
