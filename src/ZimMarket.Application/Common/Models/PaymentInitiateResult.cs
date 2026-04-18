namespace ZimMarket.Application.Common.Models;

public sealed record PaymentInitiateResult
{
    public required bool Success { get; init; }

    public string? RedirectUrl { get; init; }

    /// <summary>Paynow <c>pollurl</c> (or equivalent) for server-side status polling.</summary>
    public string? PollUrl { get; init; }

    public string? ExternalPaymentId { get; init; }

    public string? ClientSecret { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }
}
