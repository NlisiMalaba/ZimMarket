namespace ZimMarket.Application.Common.Models;

/// <summary>
/// Result of polling a payment provider for the latest transaction status (e.g. Paynow <c>pollurl</c>).
/// </summary>
public sealed record PaymentPollResult
{
    public required bool Success { get; init; }

    /// <summary>Provider-specific status string (e.g. Paid, Cancelled).</summary>
    public string? Status { get; init; }

    public Guid? OrderId { get; init; }

    public string? PaymentReference { get; init; }

    public string? ErrorMessage { get; init; }

    /// <summary>False when the response hash did not match (caller must not trust fields).</summary>
    public bool HashValid { get; init; }
}
