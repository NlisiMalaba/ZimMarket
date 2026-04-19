using ZimMarket.Domain.Enums;

namespace ZimMarket.Domain.Events;

public sealed record OrderDeliveredEvent(
    Guid OrderId,
    Guid CustomerId,
    string DeliveryPhotoKey,
    decimal TotalAmount,
    Currency TotalCurrency) : IDomainEvent;
