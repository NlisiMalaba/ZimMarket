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

public sealed class GetDashboardStatsQueryHandlerTests
{
    [Fact]
    public async Task Non_admin_returns_forbidden()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Customer);

        var handler = new GetDashboardStatsQueryHandler(
            unitOfWork,
            currentUser,
            NullLogger<GetDashboardStatsQueryHandler>.Instance);

        var result = await handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AdminDashboardErrorCodes.Forbidden);
        await unitOfWork.DashboardStats.DidNotReceive()
            .GetOperationalAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Admin_maps_operational_stats()
    {
        var raw = new DashboardStatsRaw(
            OrdersTodayCount: 4,
            PendingSellersCount: 2,
            PendingDriversCount: 1,
            ActiveDriversCount: 2,
            LowStockProductsCount: 7);

        var dashboardReads = Substitute.For<IDashboardStatsReadRepository>();
        dashboardReads.GetOperationalAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                AdminDashboardConstants.LowStockMaxQuantityInclusive,
                Arg.Any<CancellationToken>())
            .Returns(raw);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.DashboardStats.Returns(dashboardReads);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);

        var handler = new GetDashboardStatsQueryHandler(
            unitOfWork,
            currentUser,
            NullLogger<GetDashboardStatsQueryHandler>.Instance);

        var result = await handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OrdersToday.Should().Be(4);
        result.Value.PendingSellers.Should().Be(2);
        result.Value.PendingDrivers.Should().Be(1);
        result.Value.ActiveDrivers.Should().Be(2);
        result.Value.LowStockProducts.Should().Be(7);
    }
}
