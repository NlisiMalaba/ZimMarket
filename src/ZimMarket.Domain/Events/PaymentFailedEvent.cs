namespace ZimMarket.Domain.Events;

public sealed record PaymentFailedEvent(Guid OrderId, string ProviderPaymentReference, string? Reason) : IDomainEvent;
