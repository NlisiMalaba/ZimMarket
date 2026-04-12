using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Common.Behaviours;

public sealed class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(
        ICurrentUser currentUser,
        ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUser.UserId;
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            _logger.LogInformation(
                "MediatR request completed: {RequestName} {UserId} {DurationMs} {Outcome}",
                requestName,
                userId,
                sw.ElapsedMilliseconds,
                DescribeOutcome(response));

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

            _logger.LogError(
                ex,
                "MediatR request failed: {RequestName} {UserId} {DurationMs} {Outcome}",
                requestName,
                userId,
                sw.ElapsedMilliseconds,
                "Failure");

            throw;
        }
    }

    private static string DescribeOutcome(TResponse? response)
    {
        if (response is Result r)
            return r.IsSuccess ? "Success" : "Failure";

        if (response is not null)
        {
            var type = response.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var isSuccess = (bool?)type.GetProperty(nameof(Result<object>.IsSuccess))?.GetValue(response);
                return isSuccess == true ? "Success" : "Failure";
            }
        }

        return "Success";
    }
}
