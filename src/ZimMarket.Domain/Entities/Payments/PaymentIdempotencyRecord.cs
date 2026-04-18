using ZimMarket.Domain.Enums;

namespace ZimMarket.Domain.Entities.Payments;

/// <summary>
/// Stores the outcome of a payment initiation keyed by the client <c>Idempotency-Key</c> header for safe retries.
/// </summary>
public sealed class PaymentIdempotencyRecord : BaseEntity
{
    private PaymentIdempotencyRecord()
    {
    }

    public string IdempotencyKey { get; private set; } = null!;

    public Guid OrderId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string GatewayReference { get; private set; } = null!;

    public string PaymentUrl { get; private set; } = null!;

    public PaymentMethod PaymentMethod { get; private set; }

    public static PaymentIdempotencyRecord Create(
        Guid id,
        string idempotencyKey,
        Guid orderId,
        Guid customerId,
        string gatewayReference,
        string paymentUrl,
        PaymentMethod paymentMethod,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentUrl);

        return new PaymentIdempotencyRecord
        {
            Id = id,
            IdempotencyKey = idempotencyKey.Trim(),
            OrderId = orderId,
            CustomerId = customerId,
            GatewayReference = gatewayReference.Trim(),
            PaymentUrl = paymentUrl.Trim(),
            PaymentMethod = paymentMethod,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
