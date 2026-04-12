namespace ZimMarket.Domain.Events;

public sealed record DeliveryCompletedEvent(Guid BatchId, Guid DriverId) : IDomainEvent;
