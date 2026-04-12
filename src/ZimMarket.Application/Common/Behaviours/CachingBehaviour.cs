using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Common.Behaviours;

public sealed class CachingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CachingBehaviour<TRequest, TResponse>> _logger;

    public CachingBehaviour(
        IServiceProvider serviceProvider,
        ILogger<CachingBehaviour<TRequest, TResponse>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheable cacheable)
            return await next();

        var cache = _serviceProvider.GetService<ICacheService>();
        if (cache is null)
        {
            _logger.LogWarning(
                "ICacheService is not registered; skipping cache for {RequestType}",
                typeof(TRequest).Name);

            return await next();
        }

        TResponse? cached = default;
        try
        {
            cached = await cache.GetAsync<TResponse>(cacheable.CacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Cache read failed for {CacheKey}",
                cacheable.CacheKey);
        }

        if (cached is not null && ShouldReturnCached(cached))
            return cached;

        var response = await next();

        if (!ShouldWriteToCache(response))
            return response;

        try
        {
            await cache.SetAsync(cacheable.CacheKey, response, cacheable.Ttl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Cache write failed for {CacheKey}",
                cacheable.CacheKey);
        }

        return response;
    }

    private static bool ShouldReturnCached(TResponse cached)
    {
        if (cached is Result r)
            return r.IsSuccess;

        var type = cached?.GetType();
        if (type?.IsGenericType == true && type.GetGenericTypeDefinition() == typeof(Result<>))
            return (bool)type.GetProperty(nameof(Result<object>.IsSuccess))!.GetValue(cached)!;

        return true;
    }

    private static bool ShouldWriteToCache(TResponse response)
    {
        if (response is Result r)
            return r.IsSuccess;

        if (response is null)
            return false;

        var type = response.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
            return (bool)type.GetProperty(nameof(Result<object>.IsSuccess))!.GetValue(response)!;

        return true;
    }
}
