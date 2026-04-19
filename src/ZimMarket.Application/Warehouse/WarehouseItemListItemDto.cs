using ZimMarket.Domain.Enums;
using ZimMarket.Domain.ReadModels;

namespace ZimMarket.Application.Warehouse;

public sealed record WarehouseItemListItemDto(
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
    DateTimeOffset OrderCreatedAt)
{
    public static WarehouseItemListItemDto FromRow(WarehouseItemListRow r) =>
        new(
            r.WarehouseItemId,
            r.OrderId,
            r.CustomerId,
            r.ProductId,
            r.ArrivedAt,
            r.QcStatus,
            r.QcNotes,
            r.BatchId,
            r.WarehouseItemCreatedAt,
            r.OrderStatus,
            r.OrderPaymentStatus,
            r.OrderTotalAmount,
            r.OrderTotalCurrency,
            r.OrderCreatedAt);
}
