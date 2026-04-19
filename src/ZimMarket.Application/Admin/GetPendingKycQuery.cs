using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Domain.Enums;
using ZimMarket.Shared;

namespace ZimMarket.Application.Admin;

public sealed record GetPendingKycQuery(UserRole Role, int Page, int PageSize)
    : IQuery<PagedList<PendingKycQueueItemDto>>;
