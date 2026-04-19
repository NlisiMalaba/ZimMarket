using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Logistics;

public sealed record DeliveryBatchDetailDto(
    Guid BatchId,
    Guid DriverId,
    Guid WarehouseId,
    DeliveryBatchStatus Status,
    IReadOnlyList<Guid> OrderIds,
    DateTimeOffset? CollectedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
