using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Logistics;

public sealed record DeliveryBatchListItemDto(
    Guid BatchId,
    Guid DriverId,
    Guid WarehouseId,
    DeliveryBatchStatus Status,
    int OrderCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
