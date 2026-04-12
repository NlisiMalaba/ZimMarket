namespace ZimMarket.Shared;

public sealed class Result<T>
{
    private Result(T? value, IReadOnlyList<string> errors, bool isSuccess)
    {
        Value = value;
        Errors = errors;
        IsSuccess = isSuccess;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T? Value { get; }

    public IReadOnlyList<string> Errors { get; }

    public static Result<T> Success(T value) => new(value, [], true);

    public static Result<T> Failure(string error) => new(default, [error], false);

    public static Result<T> Failure(IReadOnlyList<string> errors) =>
        errors.Count > 0 ? new(default, errors, false) : Failure("Validation failed.");
}
