namespace ZimMarket.Domain.Events;

public sealed record DriverRejectedEvent(Guid DriverId, string Reason) : IDomainEvent;
