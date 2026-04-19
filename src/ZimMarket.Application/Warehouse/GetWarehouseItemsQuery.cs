using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Domain.Enums;
using ZimMarket.Shared;

namespace ZimMarket.Application.Warehouse;

public sealed record GetWarehouseItemsQuery(
    WarehouseQcStatus? QcStatus,
    int Page,
    int PageSize) : IQuery<PagedList<WarehouseItemListItemDto>>;
