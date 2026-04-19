using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using ZimMarket.Application.Common.Interfaces;

namespace ZimMarket.API.Middleware;

/// <summary>
/// Replays prior HTTP responses for duplicate <c>Idempotency-Key</c> values on selected POST routes,
/// using Redis (<see cref="ICacheService"/>) with a 24-hour TTL.
/// </summary>
public sealed class IdempotencyMiddleware
{
    public const string IdempotencyKeyHeaderName = "Idempotency-Key";

    private const int IdempotencyKeyMaxLength = 128;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<IdempotencyMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!TryGetIdempotentOperation(context, out string? routeKey, out string? idempotencyKey))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        ICacheService? cache = scope.ServiceProvider.GetService<ICacheService>();
        if (cache is null)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        string? userId = ResolveUserId(context.User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        string cacheKey = BuildCacheKey(routeKey, userId, idempotencyKey);
        IdempotencyResponseRecord? cached = await cache
            .GetAsync<IdempotencyResponseRecord>(cacheKey, context.RequestAborted)
            .ConfigureAwait(false);

        if (cached is not null)
        {
            _logger.LogInformation(
                "Idempotency cache hit for route {RouteKey} and user {UserIdPrefix}.",
                routeKey,
                userId.Length >= 8 ? userId[..8] : userId);

            await WriteCachedResponseAsync(context, cached).ConfigureAwait(false);
            return;
        }

        Stream originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch
        {
            context.Response.Body = originalBody;
            throw;
        }

        byte[] bytes = buffer.ToArray();
        context.Response.Body = originalBody;

        if (bytes.Length > 0)
            await originalBody.WriteAsync(bytes, context.RequestAborted).ConfigureAwait(false);

        if (ShouldCacheResponse(context.Response.StatusCode))
        {
            var record = new IdempotencyResponseRecord
            {
                StatusCode = context.Response.StatusCode,
                ContentType = context.Response.ContentType,
                Location = context.Response.Headers.Location.ToString(),
                BodyBase64 = bytes.Length == 0 ? string.Empty : Convert.ToBase64String(bytes)
            };

            await cache.SetAsync(cacheKey, record, CacheTtl, context.RequestAborted).ConfigureAwait(false);
        }
    }

    private static bool TryGetIdempotentOperation(
        HttpContext context,
        [NotNullWhen(true)] out string? routeKey,
        [NotNullWhen(true)] out string? idempotencyKey)
    {
        routeKey = null;
        idempotencyKey = null;

        if (!HttpMethods.IsPost(context.Request.Method))
            return false;

        PathString path = context.Request.Path;
        if (path.Equals("/api/v1/orders", StringComparison.OrdinalIgnoreCase))
            routeKey = "orders:create";
        else if (path.Equals("/api/v1/payments/initiate", StringComparison.OrdinalIgnoreCase))
            routeKey = "payments:initiate";
        else
            return false;

        string? key = context.Request.Headers[IdempotencyKeyHeaderName].FirstOrDefault()?.Trim();
        if (string.IsNullOrEmpty(key) || key.Length > IdempotencyKeyMaxLength)
            return false;

        idempotencyKey = key;
        return true;
    }

    private static string BuildCacheKey(string routeKey, string userId, string idempotencyKey) =>
        $"idempotency:{routeKey}:{userId}:{idempotencyKey}";

    private static string? ResolveUserId(ClaimsPrincipal user)
    {
        string? sub = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrWhiteSpace(sub) ? null : sub;
    }

    private static bool ShouldCacheResponse(int statusCode) => statusCode is < 500 and not 429;

    private static async Task WriteCachedResponseAsync(HttpContext context, IdempotencyResponseRecord record)
    {
        context.Response.StatusCode = record.StatusCode;

        if (!string.IsNullOrWhiteSpace(record.ContentType))
            context.Response.ContentType = record.ContentType;

        if (!string.IsNullOrWhiteSpace(record.Location))
            context.Response.Headers.Location = record.Location;

        if (string.IsNullOrEmpty(record.BodyBase64))
            return;

        byte[] body = Convert.FromBase64String(record.BodyBase64);
        await context.Response.Body.WriteAsync(body, context.RequestAborted).ConfigureAwait(false);
    }
}

