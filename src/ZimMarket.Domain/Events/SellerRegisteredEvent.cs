namespace ZimMarket.Domain.Events;

public sealed record SellerRegisteredEvent(Guid SellerId) : IDomainEvent;
