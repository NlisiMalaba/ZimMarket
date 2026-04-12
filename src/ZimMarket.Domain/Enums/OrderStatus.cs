namespace ZimMarket.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    AtWarehouse = 2,
    QcPassed = 3,
    Batched = 4,
    OutForDelivery = 5,
    Delivered = 6,
    Cancelled = 7,
    Refunded = 8
}
