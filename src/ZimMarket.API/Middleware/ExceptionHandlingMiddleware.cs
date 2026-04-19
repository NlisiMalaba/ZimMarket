using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Hosting;
using ZimMarket.API.Http;

namespace ZimMarket.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    public const string UnhandledErrorCode = "InternalError";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception).ConfigureAwait(false);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        string traceId = HttpTraceId.Get(context);

        if (context.Response.HasStarted)
        {
            using (_logger.BeginScope(new Dictionary<string, object?> { ["TraceId"] = traceId }))
                _logger.LogError(exception, "Unhandled exception after the response had started.");

            ExceptionDispatchInfo.Capture(exception).Throw();
            return;
        }

        using (_logger.BeginScope(new Dictionary<string, object?> { ["TraceId"] = traceId }))
            _logger.LogError(exception, "Unhandled exception.");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        string message = _environment.IsDevelopment()
            ? (string.IsNullOrWhiteSpace(exception.Message) ? exception.GetType().Name : exception.Message)
            : "An unexpected error occurred.";

        var body = new ApiErrorResponse(UnhandledErrorCode, message, traceId, null);
        await context.Response.WriteAsJsonAsync(body, cancellationToken: context.RequestAborted).ConfigureAwait(false);
    }
}
