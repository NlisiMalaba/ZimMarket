using System.Diagnostics.CodeAnalysis;

namespace ZimMarket.Application.Common.Models;

internal static class ResultValidationFactory
{
    public static bool TryCreateValidationFailure<TResponse>(
        List<ValidationError> errors,
        [NotNullWhen(true)] out TResponse? response)
    {
        var t = typeof(TResponse);

        if (t == typeof(Result))
        {
            response = (TResponse)(object)Result.ValidationFailure(errors);
            return true;
        }

        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var closed = typeof(Result<>).MakeGenericType(t.GetGenericArguments()[0]);
            var method = closed.GetMethod(
                nameof(Result<object>.ValidationFailure),
                [typeof(List<ValidationError>)]);
            response = (TResponse)method!.Invoke(null, [errors])!;
            return true;
        }

        response = default;
        return false;
    }
}
