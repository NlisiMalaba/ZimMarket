using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Orders;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Orders;

public sealed class OrderHandlersTests
{
    private static readonly Address TestPickupAddress =
        Address.Create("1 Pickup St", "Suburb", "Harare", "Zimbabwe").Value!;

    private static readonly Address TestDeliveryAddress =
        Address.Create("2 Delivery Rd", "Suburb", "Bulawayo", "Zimbabwe").Value!;

    private static readonly PlaceOrderDeliveryAddressDto DeliveryDto = new(
        TestDeliveryAddress.Street,
        TestDeliveryAddress.Suburb,
        TestDeliveryAddress.City,
        TestDeliveryAddress.Country);

    [Fact]
    public async Task PlaceOrder_out_of_stock_returns_PRODUCT_OUT_OF_STOCK()
    {
        Guid productId = Guid.NewGuid();
        Product product = CreateProduct(productId, stockQuantity: 1, unitPriceUsd: 5m);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var products = Substitute.For<IProductRepository>();
        unitOfWork.Products.Returns(products);
        products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new PlaceOrderCommandHandler(
            unitOfWork,
            CreateCustomerCurrentUser(),
            Substitute.For<IExchangeRateService>(),
            NullLogger<PlaceOrderCommandHandler>.Instance);

        var result = await handler.Handle(
            new PlaceOrderCommand(
                [new PlaceOrderItemDto(productId, 2)],
                DeliveryDto,
                PaymentMethod.Paynow),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OrderErrorCodes.ProductOutOfStock);
        product.StockQuantity.Should().Be(1);
        await unitOfWork.Orders.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrder_success_decrements_stock_and_raises_OrderPlacedEvent()
    {
        Guid customerId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        Product product = CreateProduct(productId, stockQuantity: 10, unitPriceUsd: 3m);

        Order? capturedOrder = null;

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var products = Substitute.For<IProductRepository>();
        var orders = Substitute.For<IOrderRepository>();
        unitOfWork.Products.Returns(products);
        unitOfWork.Orders.Returns(orders);
        products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);
        orders
            .AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => capturedOrder = ci.Arg<Order>());

        var exchangeRates = Substitute.For<IExchangeRateService>();
        exchangeRates.GetUsdToZwlAsync(Arg.Any<CancellationToken>()).Returns(26m);

        var handler = new PlaceOrderCommandHandler(
            unitOfWork,
            CreateCustomerCurrentUser(customerId),
            exchangeRates,
            NullLogger<PlaceOrderCommandHandler>.Instance);

        var result = await handler.Handle(
            new PlaceOrderCommand(
                [new PlaceOrderItemDto(productId, 3)],
                DeliveryDto,
                PaymentMethod.Ecocash),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.StockQuantity.Should().Be(7);
        await products.Received(1).UpdateAsync(product, Arg.Any<CancellationToken>());
        await orders.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());

        capturedOrder.Should().NotBeNull();
        capturedOrder!.PopDomainEvents().Should().ContainSingle(e => e is OrderPlacedEvent);
    }

    [Fact]
    public async Task CancelOrder_delivered_returns_ORDER_CANNOT_CANCEL()
    {
        Guid customerId = Guid.NewGuid();
        Order order = CreateDeliveredOrder(customerId);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orders = Substitute.For<IOrderRepository>();
        var products = Substitute.For<IProductRepository>();
        unitOfWork.Orders.Returns(orders);
        unitOfWork.Products.Returns(products);
        orders.GetByIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new CancelOrderCommandHandler(
            unitOfWork,
            CreateCustomerCurrentUser(customerId),
            NullLogger<CancelOrderCommandHandler>.Instance);

        var result = await handler.Handle(
            new CancelOrderCommand(order.Id, "Changed my mind"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OrderErrorCodes.OrderCannotCancel);
        await products.DidNotReceive()
            .UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelOrder_pending_restores_stock()
    {
        Guid customerId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        Product product = CreateProduct(productId, stockQuantity: 5, unitPriceUsd: 10m);

        var orderItem = OrderItem.Create(productId, product.Title, product.Price, quantity: 2).Value!;
        var order = Order.Create(
            Guid.NewGuid(),
            customerId,
            [orderItem],
            TestDeliveryAddress,
            Money.Create(20m, Currency.USD).Value!,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orders = Substitute.For<IOrderRepository>();
        var products = Substitute.For<IProductRepository>();
        unitOfWork.Orders.Returns(orders);
        unitOfWork.Products.Returns(products);
        orders.GetByIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new CancelOrderCommandHandler(
            unitOfWork,
            CreateCustomerCurrentUser(customerId),
            NullLogger<CancelOrderCommandHandler>.Instance);

        var result = await handler.Handle(
            new CancelOrderCommand(order.Id, "No longer needed"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.StockQuantity.Should().Be(7);
        await products.Received(1).UpdateAsync(product, Arg.Any<CancellationToken>());
        await orders.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
    }

    private static ICurrentUser CreateCustomerCurrentUser(Guid? userId = null)
    {
        Guid id = userId ?? Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(id);
        currentUser.Role.Returns(UserRole.Customer);
        return currentUser;
    }

    private static Product CreateProduct(Guid id, int stockQuantity, decimal unitPriceUsd)
    {
        var price = Money.Create(unitPriceUsd, Currency.USD).Value!;
        return Product.Create(
            id,
            sellerId: Guid.NewGuid(),
            title: "Test product",
            description: "Test description for product listing.",
            price,
            categoryId: Guid.NewGuid(),
            stockQuantity,
            imageKeys: [],
            pickupAddress: TestPickupAddress,
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow).Value!;
    }

    private static Order CreateDeliveredOrder(Guid customerId)
    {
        Guid productId = Guid.NewGuid();
        var price = Money.Create(1m, Currency.USD).Value!;
        var productTitle = "Line item";
        var orderItem = OrderItem.Create(productId, productTitle, price, quantity: 1).Value!;
        var order = Order.Create(
            Guid.NewGuid(),
            customerId,
            [orderItem],
            TestDeliveryAddress,
            Money.Create(1m, Currency.USD).Value!,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;

        order.ConfirmPayment("pay-ref");
        order.UpdateStatus(OrderStatus.AtWarehouse);
        order.UpdateStatus(OrderStatus.QcPassed);
        order.UpdateStatus(OrderStatus.Batched);
        order.UpdateStatus(OrderStatus.OutForDelivery);
        order.UpdateStatus(OrderStatus.Delivered);
        return order;
    }
}
