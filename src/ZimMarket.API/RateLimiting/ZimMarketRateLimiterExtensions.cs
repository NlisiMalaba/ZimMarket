using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using ZimMarket.API.Http;
using ZimMarket.Infrastructure.BackgroundJobs;

namespace ZimMarket.API.RateLimiting;

public static class ZimMarketRateLimiterExtensions
{
    public static IServiceCollection AddZimMarketRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                if (ShouldBypass(httpContext))
                    return RateLimitPartition.GetNoLimiter("bypass");

                string ip = ResolveClientIp(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 200,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            options.AddPolicy(ZimMarketRateLimitPolicies.AuthByIp, httpContext =>
            {
                string ip = ResolveClientIp(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            options.AddPolicy(ZimMarketRateLimitPolicies.PresignByUser, httpContext =>
            {
                string partitionKey = ResolvePresignPartitionKey(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                HttpContext httpContext = context.HttpContext;
                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    int seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                    httpContext.Response.Headers.RetryAfter = seconds.ToString(NumberFormatInfo.InvariantInfo);
                }

                if (httpContext.Response.HasStarted)
                    return;

                httpContext.Response.ContentType = "application/json";
                string traceId = HttpTraceId.Get(httpContext);
                var body = new ApiErrorResponse("TooManyRequests", "Too many requests. Please try again later.", traceId, null);
                await httpContext.Response.WriteAsJsonAsync(body, cancellationToken).ConfigureAwait(false);
            };
        });

        return services;
    }

    private static bool ShouldBypass(HttpContext httpContext)
    {
        PathString path = httpContext.Request.Path;
        return path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments(HangfireJobSetup.DashboardPath, StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string ResolvePresignPartitionKey(HttpContext httpContext)
    {
        string? sub = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrWhiteSpace(sub))
            return "user:" + sub;

        return "ip:" + ResolveClientIp(httpContext);
    }
}
