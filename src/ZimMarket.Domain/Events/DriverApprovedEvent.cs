namespace ZimMarket.Domain.Events;

public sealed record DriverApprovedEvent(Guid DriverId) : IDomainEvent;
