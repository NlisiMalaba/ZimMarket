using ZimMarket.Domain.Enums;

namespace ZimMarket.Domain.Events;

/// <summary>Emitted when an administrator forces an order to a new <see cref="OrderStatus"/> outside the normal transition map.</summary>
public sealed record OrderStatusAdminOverriddenEvent(
    Guid OrderId,
    OrderStatus PreviousStatus,
    OrderStatus NewStatus,
    string Reason) : IDomainEvent;
