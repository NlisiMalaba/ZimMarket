namespace ZimMarket.Domain.Events;

public sealed record BatchCollectedEvent(Guid BatchId) : IDomainEvent;
