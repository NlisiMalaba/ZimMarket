using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.API.Http;

public static class ResultHttpMapper
{
    public static IActionResult ToOkActionResult(this Result result, HttpContext httpContext)
    {
        if (result.IsSuccess)
            return new OkObjectResult(new ApiSuccessResponse<object?>(null));

        return ToErrorObjectResult(result.ErrorCode, result.ErrorMessage, result.ValidationErrors, httpContext);
    }

    public static IActionResult ToOkActionResult<T>(this Result<T> result, HttpContext httpContext)
    {
        if (result.IsSuccess)
            return new OkObjectResult(new ApiSuccessResponse<T>(result.Value));

        return ToErrorObjectResult(result.ErrorCode, result.ErrorMessage, result.ValidationErrors, httpContext);
    }

    public static IActionResult ToCreatedActionResult<T>(this Result<T> result, HttpContext httpContext)
    {
        if (result.IsSuccess)
            return new ObjectResult(new ApiSuccessResponse<T>(result.Value)) { StatusCode = StatusCodes.Status201Created };

        return ToErrorObjectResult(result.ErrorCode, result.ErrorMessage, result.ValidationErrors, httpContext);
    }

    private static ObjectResult ToErrorObjectResult(
        string? errorCode,
        string? errorMessage,
        List<ValidationError>? validationErrors,
        HttpContext httpContext)
    {
        string code = string.IsNullOrWhiteSpace(errorCode) ? "Error" : errorCode;
        string message = string.IsNullOrWhiteSpace(errorMessage) ? "Request failed." : errorMessage;

        int statusCode = MapStatusCode(code);
        IReadOnlyList<ApiValidationErrorItem>? items = validationErrors is { Count: > 0 }
            ? validationErrors.Select(e => new ApiValidationErrorItem(e.Field, e.Message)).ToList()
            : null;

        var body = new ApiErrorResponse(code, message, GetTraceId(httpContext), items);
        return new ObjectResult(body) { StatusCode = statusCode };
    }

    private static string GetTraceId(HttpContext httpContext) =>
        Activity.Current?.Id ?? httpContext.TraceIdentifier;

    private static int MapStatusCode(string errorCode)
    {
        if (errorCode == Result.ValidationErrorCode)
            return StatusCodes.Status422UnprocessableEntity;

        return errorCode switch
        {
            "Auth.InvalidCredentials" => StatusCodes.Status401Unauthorized,
            "Auth.InvalidRefreshToken" => StatusCodes.Status401Unauthorized,
            "Auth.InvalidAccessToken" => StatusCodes.Status401Unauthorized,
            "Auth.AccountDisabled" => StatusCodes.Status403Forbidden,
            "Auth.EmailTaken" => StatusCodes.Status409Conflict,
            "Auth.PhoneTaken" => StatusCodes.Status409Conflict,
            "Kyc.Forbidden" => StatusCodes.Status403Forbidden,
            "Kyc.SellerNotFound" => StatusCodes.Status404NotFound,
            "Kyc.DriverNotFound" => StatusCodes.Status404NotFound,
            "Kyc.AlreadySubmitted" => StatusCodes.Status409Conflict,
            "Auth.AccessTokenNotExpired" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };
    }
}
