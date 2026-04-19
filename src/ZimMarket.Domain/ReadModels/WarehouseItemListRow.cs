using ZimMarket.Domain.Enums;

namespace ZimMarket.Domain.ReadModels;

/// <summary>Joined warehouse item + order summary for admin warehouse lists.</summary>
public sealed record WarehouseItemListRow(
    Guid WarehouseItemId,
    Guid OrderId,
    Guid CustomerId,
    Guid ProductId,
    DateTimeOffset ArrivedAt,
    WarehouseQcStatus QcStatus,
    string? QcNotes,
    Guid? BatchId,
    DateTimeOffset WarehouseItemCreatedAt,
    OrderStatus OrderStatus,
    PaymentStatus OrderPaymentStatus,
    decimal OrderTotalAmount,
    Currency OrderTotalCurrency,
    DateTimeOffset OrderCreatedAt);
