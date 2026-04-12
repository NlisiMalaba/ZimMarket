namespace ZimMarket.Domain.Events;

public sealed record DriverLocationUpdatedEvent(
    Guid DriverId,
    double Lat,
    double Lng,
    List<Guid> ActiveOrderIds) : IDomainEvent;