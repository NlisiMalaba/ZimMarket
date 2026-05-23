using ZimMarket.Application.Common.Abstractions;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Sellers;

public sealed record GetSellerDashboardStatsQuery : IQuery<SellerDashboardStatsDto>;
