namespace ZimMarket.Domain.Events;

public sealed record ItemArrivedAtWarehouseEvent(Guid OrderId, Guid WarehouseItemId) : IDomainEvent;
