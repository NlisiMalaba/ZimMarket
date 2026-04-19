using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ReadModels;
using ZimMarket.Shared;

namespace ZimMarket.Application.Tests.Orders;

public sealed class GetAllOrdersQueryHandlerTests
{
    [Fact]
    public async Task Non_admin_returns_forbidden()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Customer);

        var handler = new GetAllOrdersQueryHandler(
            unitOfWork,
            currentUser,
            NullLogger<GetAllOrdersQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetAllOrdersQuery(null, null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AdminOrderErrorCodes.Forbidden);
        await unitOfWork.Orders.DidNotReceive()
            .GetAllPagedForAdminAsync(
                Arg.Any<OrderStatus?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<PaginationParams>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Admin_returns_mapped_page()
    {
        Guid orderId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();
        var row = new OrderListAdminRow(
            orderId,
            customerId,
            OrderStatus.Paid,
            PaymentStatus.Paid,
            99.50m,
            Currency.USD,
            LineItemCount: 2,
            CreatedAt: DateTimeOffset.Parse("2026-04-01T12:00:00Z"));

        var page = new PagedList<OrderListAdminRow>([row], 1, 20, 1);

        var orders = Substitute.For<IOrderRepository>();
        orders.GetAllPagedForAdminAsync(
                OrderStatus.Paid,
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<PaginationParams>(),
                Arg.Any<CancellationToken>())
            .Returns(page);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Orders.Returns(orders);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);

        var handler = new GetAllOrdersQueryHandler(
            unitOfWork,
            currentUser,
            NullLogger<GetAllOrdersQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetAllOrdersQuery(OrderStatus.Paid, null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        AdminOrderListItemDto dto = result.Value.Items[0];
        dto.OrderId.Should().Be(orderId);
        dto.CustomerId.Should().Be(customerId);
        dto.Status.Should().Be(OrderStatus.Paid);
        dto.LineItemCount.Should().Be(2);
        dto.TotalCurrency.Should().Be("USD");
    }
}
