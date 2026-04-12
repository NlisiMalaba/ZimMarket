namespace ZimMarket.Application.Common.Abstractions;

/// <summary>
/// Marks a query request whose response may be cached.
/// </summary>
public interface ICacheable
{
    string CacheKey { get; }

    TimeSpan Ttl { get; }
}
