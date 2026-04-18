namespace ZimMarket.API.Http;

public sealed record ApiSuccessResponse<T>(T? Data);

public sealed record ApiErrorResponse(
    string ErrorCode,
    string Message,
    string TraceId,
    IReadOnlyList<ApiValidationErrorItem>? ValidationErrors);

public sealed record ApiValidationErrorItem(string Field, string Message);
