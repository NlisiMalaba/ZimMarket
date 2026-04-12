namespace ZimMarket.Application.Common.Models;

public sealed record PaymentRequest
{
    public required Guid OrderId { get; init; }

    public required decimal Amount { get; init; }

    public required string Currency { get; init; }

    public string? Description { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
