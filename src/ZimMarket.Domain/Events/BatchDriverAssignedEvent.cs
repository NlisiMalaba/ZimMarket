namespace ZimMarket.Domain.Events;

public sealed record BatchDriverAssignedEvent(Guid BatchId, Guid DriverId) : IDomainEvent;
