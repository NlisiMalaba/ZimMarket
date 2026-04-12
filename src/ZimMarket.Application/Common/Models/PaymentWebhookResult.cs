namespace ZimMarket.Application.Common.Models;

public sealed record PaymentWebhookResult
{
    public required bool IsValid { get; init; }

    public Guid? OrderId { get; init; }

    public string? PaymentReference { get; init; }

    public string? Status { get; init; }

    public string? ErrorMessage { get; init; }
}
