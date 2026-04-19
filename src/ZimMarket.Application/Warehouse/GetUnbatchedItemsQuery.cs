using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Warehouse;

/// <summary>QC-passed warehouse rows not yet assigned to a delivery batch (batch creation UI).</summary>
public sealed record GetUnbatchedItemsQuery : IQuery<IReadOnlyList<WarehouseItemListItemDto>>;
