namespace ZimMarket.Domain.Events;

public sealed record DriverRegisteredEvent(Guid DriverId) : IDomainEvent;
