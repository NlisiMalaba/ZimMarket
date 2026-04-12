namespace ZimMarket.Domain.Events;

public sealed record PaymentConfirmedEvent(Guid OrderId, string Reference) : IDomainEvent;
