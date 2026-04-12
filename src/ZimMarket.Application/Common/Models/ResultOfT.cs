namespace ZimMarket.Application.Common.Models;

public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, string? errorCode, string? errorMessage, List<ValidationError>? validationErrors)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public List<ValidationError>? ValidationErrors { get; }

    public static Result<T> Success(T value) => new(true, value, null, null, null);

    public static Result<T> Failure(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new Result<T>(false, default, code, message, null);
    }

    public static Result<T> ValidationFailure(List<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var copy = new List<ValidationError>(errors);
        return new Result<T>(false, default, Result.ValidationErrorCode, Result.FormatValidationSummary(copy), copy);
    }
}
