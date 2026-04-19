namespace ZimMarket.Domain.Events;

public sealed record BatchCreatedEvent(
    Guid BatchId,
    Guid DriverId,
    Guid PickupWarehouseId,
    IReadOnlyList<Guid> OrderIds) : IDomainEvent;
