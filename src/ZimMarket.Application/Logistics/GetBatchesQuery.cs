using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Domain.Enums;
using ZimMarket.Shared;

namespace ZimMarket.Application.Logistics;

public sealed record GetBatchesQuery(
    DeliveryBatchStatus? Status,
    int Page,
    int PageSize) : IQuery<PagedList<DeliveryBatchListItemDto>>;
