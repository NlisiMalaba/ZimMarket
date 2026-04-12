namespace ZimMarket.Domain.Events;

public sealed record SellerApprovedEvent(Guid SellerId) : IDomainEvent;
