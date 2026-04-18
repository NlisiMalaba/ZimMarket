namespace ZimMarket.Domain.Events;

public sealed record OrderCancelledEvent(Guid OrderId, Guid CustomerId, string Reason) : IDomainEvent;
