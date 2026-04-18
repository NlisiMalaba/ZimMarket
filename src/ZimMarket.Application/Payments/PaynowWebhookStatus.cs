namespace ZimMarket.Application.Payments;

/// <summary>
/// Interprets Paynow <c>status</c> values from result URL / webhook posts (see Paynow status update documentation).
/// </summary>
internal static class PaynowWebhookStatus
{
    /// <summary>Statuses that mean funds are committed / received and the order should be marked paid.</summary>
    public static bool IsPaid(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return false;

        string s = status.Trim();
        return s switch
        {
            _ when string.Equals(s, "Paid", StringComparison.OrdinalIgnoreCase) => true,
            _ when string.Equals(s, "Awaiting Delivery", StringComparison.OrdinalIgnoreCase) => true,
            _ when string.Equals(s, "Delivered", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }

    /// <summary>Statuses that mean the transaction will not complete successfully for this attempt.</summary>
    public static bool IsFailed(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return false;

        string s = status.Trim();
        return s switch
        {
            _ when string.Equals(s, "Refunded", StringComparison.OrdinalIgnoreCase) => true,
            _ when string.Equals(s, "Cancelled", StringComparison.OrdinalIgnoreCase) => true,
            _ when string.Equals(s, "Declined", StringComparison.OrdinalIgnoreCase) => true,
            _ when string.Equals(s, "Error", StringComparison.OrdinalIgnoreCase) => true,
            _ when string.Equals(s, "NotFound", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }
}
