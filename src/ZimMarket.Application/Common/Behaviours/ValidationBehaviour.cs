using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Common.Behaviours;

public sealed class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehaviour<TRequest, TResponse>> _logger;

    public ValidationBehaviour(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehaviour<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validators = _validators.ToList();
        if (validators.Count == 0)
            return await next();

        List<ValidationError> errors;
        try
        {
            var validationResults = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(request, cancellationToken)));

            errors = validationResults
                .SelectMany(r => r.Errors)
                .Select(f => new ValidationError(f.PropertyName, f.ErrorMessage))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FluentValidation threw for {RequestType}",
                typeof(TRequest).Name);

            errors =
            [
                new ValidationError(
                    string.Empty,
                    "Validation could not be completed.")
            ];
        }

        if (errors.Count == 0)
            return await next();

        if (ResultValidationFactory.TryCreateValidationFailure<TResponse>(errors, out var failureResponse))
            return failureResponse;

        _logger.LogError(
            "Validation failed for {RequestType} but response type {ResponseType} is not Result or Result<T>; configure handlers that use validators to return Result types",
            typeof(TRequest).Name,
            typeof(TResponse).Name);

        return default!;
    }
}
