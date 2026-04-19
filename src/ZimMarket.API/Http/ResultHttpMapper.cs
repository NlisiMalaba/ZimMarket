using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Models;
using ZimMarket.Application.Drivers;
using ZimMarket.Application.Logistics;

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
            AuthErrorCodes.AuthInvalidCredentials => StatusCodes.Status401Unauthorized,
            AuthErrorCodes.AuthRefreshInvalid => StatusCodes.Status401Unauthorized,
            AuthErrorCodes.AuthInvalidAccessToken => StatusCodes.Status401Unauthorized,
            AuthErrorCodes.AuthAccountLocked => StatusCodes.Status403Forbidden,
            AuthErrorCodes.UserAlreadyExists => StatusCodes.Status409Conflict,
            AuthErrorCodes.UserPhoneAlreadyExists => StatusCodes.Status409Conflict,
            OrderErrorCodes.OrderForbidden => StatusCodes.Status403Forbidden,
            OrderErrorCodes.OrderNotFound => StatusCodes.Status404NotFound,
            OrderErrorCodes.OrderCannotCancel => StatusCodes.Status409Conflict,
            OrderErrorCodes.OrderInvalidStatusForArrival => StatusCodes.Status409Conflict,
            OrderErrorCodes.OrderArrivalAlreadyRecorded => StatusCodes.Status409Conflict,
            WarehouseErrorCodes.WarehouseItemNotFound => StatusCodes.Status404NotFound,
            WarehouseErrorCodes.WarehouseQcInvalid => StatusCodes.Status409Conflict,
            WarehouseErrorCodes.WarehouseForbidden => StatusCodes.Status403Forbidden,
            WarehouseErrorCodes.OrderInvalidStatusForQc => StatusCodes.Status409Conflict,
            LogisticsErrorCodes.LogisticsForbidden => StatusCodes.Status403Forbidden,
            LogisticsErrorCodes.DriverNotFound => StatusCodes.Status404NotFound,
            LogisticsErrorCodes.DriverNotEligible => StatusCodes.Status409Conflict,
            LogisticsErrorCodes.DriverHasActiveBatch => StatusCodes.Status409Conflict,
            LogisticsErrorCodes.OrderNotEligibleForBatch => StatusCodes.Status409Conflict,
            LogisticsErrorCodes.OrderAlreadyBatched => StatusCodes.Status409Conflict,
            LogisticsErrorCodes.BatchCreateFailed => StatusCodes.Status400BadRequest,
            LogisticsErrorCodes.DeliveryBatchNotFound => StatusCodes.Status404NotFound,
            LogisticsErrorCodes.DeliveryBatchForbidden => StatusCodes.Status403Forbidden,
            LogisticsErrorCodes.DeliveryBatchInvalidState => StatusCodes.Status409Conflict,
            LogisticsErrorCodes.OrderNotBatchedForCollection => StatusCodes.Status409Conflict,
            LogisticsErrorCodes.OrderNotInDeliveryBatch => StatusCodes.Status400BadRequest,
            LogisticsErrorCodes.BatchNotReadyForDelivery => StatusCodes.Status409Conflict,
            LogisticsErrorCodes.OrderNotOutForDelivery => StatusCodes.Status409Conflict,
            LogisticsErrorCodes.OrderAlreadyDelivered => StatusCodes.Status409Conflict,
            DriverLocationErrorCodes.DriverLocationForbidden => StatusCodes.Status403Forbidden,
            DriverDeliveryErrorCodes.DriverForbidden => StatusCodes.Status403Forbidden,
            DriverLocationErrorCodes.DriverNotOnDelivery => StatusCodes.Status409Conflict,
            DriverLocationErrorCodes.DriverLocationInvalidCoordinates => StatusCodes.Status422UnprocessableEntity,
            OrderErrorCodes.ProductNotFound => StatusCodes.Status404NotFound,
            OrderErrorCodes.ProductInactive => StatusCodes.Status409Conflict,
            OrderErrorCodes.ProductOutOfStock => StatusCodes.Status409Conflict,
            "Kyc.Forbidden" => StatusCodes.Status403Forbidden,
            "Kyc.SellerNotFound" => StatusCodes.Status404NotFound,
            "Kyc.DriverNotFound" => StatusCodes.Status404NotFound,
            "Kyc.AlreadySubmitted" => StatusCodes.Status409Conflict,
            AuthErrorCodes.AuthAccessTokenNotExpired => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };
    }
}
