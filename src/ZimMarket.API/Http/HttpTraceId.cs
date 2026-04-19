using System.Diagnostics;

namespace ZimMarket.API.Http;

internal static class HttpTraceId
{
    public static string Get(HttpContext httpContext) =>
        Activity.Current?.Id ?? httpContext.TraceIdentifier;
}
