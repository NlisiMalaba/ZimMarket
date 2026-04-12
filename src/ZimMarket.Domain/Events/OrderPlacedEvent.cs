namespace ZimMarket.Domain.Events;

public sealed record OrderPlacedEvent(Guid OrderId, Guid CustomerId, decimal TotalUsd) : IDomainEvent;
