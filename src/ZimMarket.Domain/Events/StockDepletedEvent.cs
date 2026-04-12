namespace ZimMarket.Domain.Events;

public sealed record StockDepletedEvent(Guid ProductId) : IDomainEvent;
