using FluentAssertions;
using NSubstitute;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;
using ZimMarket.Infrastructure.RealTime;

namespace ZimMarket.Application.Tests.RealTime;

public sealed class TrackingHubSubscriptionServiceTests
{
    [Fact]
    public async Task CanCustomerTrackOrder_returns_true_when_order_belongs_to_customer()
    {
        Guid customerId = Guid.NewGuid();
        Guid orderId = Guid.NewGuid();
        Order order = CreateOrder(orderId, customerId);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orders = Substitute.For<IOrderRepository>();
        unitOfWork.Orders.Returns(orders);
        orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

        var service = new TrackingHubSubscriptionService(unitOfWork);

        bool allowed = await service.CanCustomerTrackOrderAsync(customerId, orderId, CancellationToken.None);

        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task CanCustomerTrackOrder_returns_false_when_order_belongs_to_another_customer()
    {
        Guid orderId = Guid.NewGuid();
        Order order = CreateOrder(orderId, Guid.NewGuid());

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orders = Substitute.For<IOrderRepository>();
        unitOfWork.Orders.Returns(orders);
        orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

        var service = new TrackingHubSubscriptionService(unitOfWork);

        bool allowed = await service.CanCustomerTrackOrderAsync(Guid.NewGuid(), orderId, CancellationToken.None);

        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task CanCustomerTrackOrder_returns_false_when_order_missing()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orders = Substitute.For<IOrderRepository>();
        unitOfWork.Orders.Returns(orders);
        orders.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Order?)null);

        var service = new TrackingHubSubscriptionService(unitOfWork);

        bool allowed = await service.CanCustomerTrackOrderAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        allowed.Should().BeFalse();
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.SuperAdmin)]
    public void CanAdminTrackDriverMap_returns_true_for_admins(UserRole role)
    {
        var service = new TrackingHubSubscriptionService(Substitute.For<IUnitOfWork>());

        service.CanAdminTrackDriverMap(role).Should().BeTrue();
    }

    [Theory]
    [InlineData(UserRole.Customer)]
    [InlineData(UserRole.Driver)]
    [InlineData(UserRole.Seller)]
    public void CanAdminTrackDriverMap_returns_false_for_non_admins(UserRole role)
    {
        var service = new TrackingHubSubscriptionService(Substitute.For<IUnitOfWork>());

        service.CanAdminTrackDriverMap(role).Should().BeFalse();
    }

    private static Order CreateOrder(Guid orderId, Guid customerId)
    {
        Address address = Address.Create("1 St", "Sub", "City", "ZW").Value!;
        Money total = Money.Create(5m, Currency.USD).Value!;
        OrderItem line = OrderItem.Create(Guid.NewGuid(), "Product", Money.Create(5m, Currency.USD).Value!, 1).Value!;
        return Order.Create(orderId, customerId, [line], address, total, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            .Value!;
    }
}
