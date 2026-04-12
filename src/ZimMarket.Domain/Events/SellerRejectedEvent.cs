namespace ZimMarket.Domain.Events;

public sealed record SellerRejectedEvent(Guid SellerId, string Reason) : IDomainEvent;
