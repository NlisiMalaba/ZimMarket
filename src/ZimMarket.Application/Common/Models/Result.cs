namespace ZimMarket.Application.Common.Models;

public sealed class Result
{
    public const string ValidationErrorCode = "Validation";

    private Result(bool isSuccess, string? errorCode, string? errorMessage, List<ValidationError>? validationErrors)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors;
    }

    public bool IsSuccess { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public List<ValidationError>? ValidationErrors { get; }

    public static Result Success() => new(true, null, null, null);

    public static Result Failure(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new Result(false, code, message, null);
    }

    public static Result ValidationFailure(List<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var copy = new List<ValidationError>(errors);
        return new Result(false, ValidationErrorCode, FormatValidationSummary(copy), copy);
    }

    internal static string FormatValidationSummary(IReadOnlyList<ValidationError> errors)
    {
        return errors.Count == 0
            ? "Validation failed."
            : string.Join("; ", errors.Select(e => $"{e.Field}: {e.Message}"));
    }
}
