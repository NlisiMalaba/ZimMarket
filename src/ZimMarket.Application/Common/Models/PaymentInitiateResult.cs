namespace ZimMarket.Application.Common.Models;

public sealed record PaymentInitiateResult
{
    public required bool Success { get; init; }

    public string? RedirectUrl { get; init; }

    public string? ExternalPaymentId { get; init; }

    public string? ClientSecret { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }
}
