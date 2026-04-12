namespace ZimMarket.Domain.Events;

public sealed record BatchInTransitEvent(Guid BatchId) : IDomainEvent;
