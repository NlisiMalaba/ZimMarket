namespace ZimMarket.Infrastructure.Configuration;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>Connection string (or <c>ConnectionStrings:Redis</c> when this is empty).</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Optional prefix prepended to every cache key and pattern (e.g. <c>ZimMarket:prod:</c>).</summary>
    public string KeyPrefix { get; set; } = string.Empty;
}
