using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Admin;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ReadModels;

namespace ZimMarket.Application.Tests.Admin;

public sealed class GetFinanceDashboardStatsQueryHandlerTests
{
    [Fact]
    public async Task Admin_returns_forbidden()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);

        var exchange = Substitute.For<IExchangeRateService>();

        var handler = new GetFinanceDashboardStatsQueryHandler(
            unitOfWork,
            currentUser,
            exchange,
            NullLogger<GetFinanceDashboardStatsQueryHandler>.Instance);

        var result = await handler.Handle(new GetFinanceDashboardStatsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AdminDashboardErrorCodes.Forbidden);
        await unitOfWork.DashboardStats.DidNotReceive()
            .GetFinanceAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Super_admin_maps_revenue_periods_in_usd()
    {
        var raw = new FinanceDashboardStatsRaw(
            PaidOrderTotalsToday: [new PaidOrderTotalRow(100m, Currency.USD)],
            PaidOrderTotalsMonth: [new PaidOrderTotalRow(50m, Currency.USD)],
            PaidOrderTotalsYear: [new PaidOrderTotalRow(260m, Currency.ZWG)],
            PaidOrderTotalsAllTime: [new PaidOrderTotalRow(10m, Currency.USD)]);

        var dashboardReads = Substitute.For<IDashboardStatsReadRepository>();
        dashboardReads.GetFinanceAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(raw);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.DashboardStats.Returns(dashboardReads);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.SuperAdmin);

        var exchange = Substitute.For<IExchangeRateService>();
        exchange.GetUsdToZwlAsync(Arg.Any<CancellationToken>()).Returns(26m);

        var handler = new GetFinanceDashboardStatsQueryHandler(
            unitOfWork,
            currentUser,
            exchange,
            NullLogger<GetFinanceDashboardStatsQueryHandler>.Instance);

        var result = await handler.Handle(new GetFinanceDashboardStatsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RevenueTodayUsd.Should().Be(100m);
        result.Value.RevenueMonthUsd.Should().Be(50m);
        result.Value.RevenueYearUsd.Should().Be(10m);
        result.Value.RevenueAllTimeUsd.Should().Be(10m);
    }
}
