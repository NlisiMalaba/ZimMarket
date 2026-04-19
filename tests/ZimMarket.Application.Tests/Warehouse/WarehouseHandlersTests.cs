using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Warehouse;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Entities.Warehouse;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ReadModels;
using ZimMarket.Domain.ValueObjects;
using ZimMarket.Shared;

namespace ZimMarket.Application.Tests.Warehouse;

public sealed class WarehouseHandlersTests
{
    private static readonly PhoneNumber TestPhone = PhoneNumber.Create("+263771234567").Value!;

    private static readonly Address TestDeliveryAddress =
        Address.Create("2 Delivery Rd", "Suburb", "Bulawayo", "Zimbabwe").Value!;

    [Fact]
    public async Task RecordItemArrival_non_admin_returns_ORDER_FORBIDDEN()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new RecordItemArrivalCommandHandler(
            unitOfWork,
            CreateCustomerCurrentUser(),
            NullLogger<RecordItemArrivalCommandHandler>.Instance);

        var result = await handler.Handle(
            new RecordItemArrivalCommand(Guid.NewGuid(), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OrderErrorCodes.OrderForbidden);
        await unitOfWork.Orders.DidNotReceive()
            .GetByIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordItemArrival_not_paid_returns_ORDER_INVALID_STATUS_FOR_ARRIVAL()
    {
        Guid orderId = Guid.NewGuid();
        var order = CreateOrderInStatus(orderId, OrderStatus.Pending);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orders = Substitute.For<IOrderRepository>();
        var warehouseItems = Substitute.For<IWarehouseItemRepository>();
        unitOfWork.Orders.Returns(orders);
        unitOfWork.WarehouseItems.Returns(warehouseItems);
        orders.GetByIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        warehouseItems.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<WarehouseItem>());

        var handler = new RecordItemArrivalCommandHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            NullLogger<RecordItemArrivalCommandHandler>.Instance);

        var result = await handler.Handle(new RecordItemArrivalCommand(orderId, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(OrderErrorCodes.OrderInvalidStatusForArrival);
        await warehouseItems.DidNotReceive().AddAsync(Arg.Any<WarehouseItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordItemArrival_paid_creates_warehouse_rows_and_transitions_order()
    {
        Guid orderId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        Order order = CreateSingleLinePaidOrder(orderId, productId, customerId: Guid.NewGuid());

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orders = Substitute.For<IOrderRepository>();
        var warehouseItems = Substitute.For<IWarehouseItemRepository>();
        unitOfWork.Orders.Returns(orders);
        unitOfWork.WarehouseItems.Returns(warehouseItems);
        orders.GetByIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        warehouseItems.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<WarehouseItem>());

        var handler = new RecordItemArrivalCommandHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            NullLogger<RecordItemArrivalCommandHandler>.Instance);

        var result = await handler.Handle(
            new RecordItemArrivalCommand(orderId, "  Received OK  "),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.AtWarehouse);
        await warehouseItems.Received(1).AddAsync(
            Arg.Is<WarehouseItem>(w =>
                w.OrderId == orderId
                && w.ProductId == productId
                && w.QcStatus == WarehouseQcStatus.Pending
                && w.QcNotes == "Received OK"),
            Arg.Any<CancellationToken>());
        await orders.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());

        order.PopDomainEvents().Should().ContainSingle(e => e is ItemArrivedAtWarehouseEvent);
    }

    [Fact]
    public async Task ItemArrivedAtWarehouseEventHandler_enqueues_push_when_customer_has_token()
    {
        Guid orderId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();
        Guid warehouseItemId = Guid.NewGuid();

        var order = CreateSingleLinePaidOrder(orderId, Guid.NewGuid(), customerId);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var customer = new Customer(
            customerId,
            "c@test.com",
            "Test Customer",
            TestPhone,
            passwordHash: "hash",
            KycStatus.NotSubmitted,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            createdAt: now,
            updatedAt: now,
            pushNotificationToken: "device-token");

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orders = Substitute.For<IOrderRepository>();
        var customers = Substitute.For<IUserRepository<Customer>>();
        unitOfWork.Orders.Returns(orders);
        unitOfWork.Customers.Returns(customers);
        orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        customers.GetByIdAsync(customerId, Arg.Any<CancellationToken>()).Returns(customer);

        var jobs = Substitute.For<INotificationJobScheduler>();
        var handler = new ItemArrivedAtWarehouseEventHandler(
            unitOfWork,
            jobs,
            NullLogger<ItemArrivedAtWarehouseEventHandler>.Instance);

        await handler.Handle(new ItemArrivedAtWarehouseEvent(orderId, warehouseItemId), CancellationToken.None);

        jobs.Received(1).EnqueuePushToToken(
            "device-token",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d["orderId"] == orderId.ToString("D")
                && d["warehouseItemId"] == warehouseItemId.ToString("D")
                && d["event"] == "item_arrived_at_warehouse"));
    }

    [Fact]
    public async Task GetWarehouseItems_non_admin_returns_WAREHOUSE_FORBIDDEN()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new GetWarehouseItemsQueryHandler(
            unitOfWork,
            CreateCustomerCurrentUser(),
            NullLogger<GetWarehouseItemsQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetWarehouseItemsQuery(null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(WarehouseErrorCodes.WarehouseForbidden);
        await unitOfWork.WarehouseItems.DidNotReceive()
            .GetPagedForAdminAsync(Arg.Any<WarehouseQcStatus?>(), Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWarehouseItems_admin_returns_mapped_page()
    {
        Guid orderId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        Guid whId = Guid.NewGuid();
        DateTimeOffset arrived = DateTimeOffset.UtcNow;
        DateTimeOffset orderCreated = arrived.AddHours(-2);

        var row = new WarehouseItemListRow(
            whId,
            orderId,
            customerId,
            productId,
            arrived,
            WarehouseQcStatus.Pending,
            "Receiving note",
            BatchId: null,
            WarehouseItemCreatedAt: arrived,
            OrderStatus.AtWarehouse,
            PaymentStatus.Paid,
            OrderTotalAmount: 42.5m,
            OrderTotalCurrency: Currency.USD,
            OrderCreatedAt: orderCreated);

        var paged = new PagedList<WarehouseItemListRow>([row], page: 1, pageSize: 20, totalCount: 1);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var warehouseItems = Substitute.For<IWarehouseItemRepository>();
        unitOfWork.WarehouseItems.Returns(warehouseItems);
        warehouseItems
            .GetPagedForAdminAsync(WarehouseQcStatus.Pending, Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>())
            .Returns(paged);

        var handler = new GetWarehouseItemsQueryHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            NullLogger<GetWarehouseItemsQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetWarehouseItemsQuery(WarehouseQcStatus.Pending, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        var dto = result.Value.Items.Single();
        dto.WarehouseItemId.Should().Be(whId);
        dto.OrderId.Should().Be(orderId);
        dto.CustomerId.Should().Be(customerId);
        dto.OrderStatus.Should().Be(OrderStatus.AtWarehouse);
        dto.OrderTotalAmount.Should().Be(42.5m);
        dto.OrderTotalCurrency.Should().Be(Currency.USD);
        dto.OrderCreatedAt.Should().Be(orderCreated);
    }

    [Fact]
    public async Task GetUnbatchedItems_non_admin_returns_WAREHOUSE_FORBIDDEN()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new GetUnbatchedItemsQueryHandler(
            unitOfWork,
            CreateCustomerCurrentUser(),
            NullLogger<GetUnbatchedItemsQueryHandler>.Instance);

        var result = await handler.Handle(new GetUnbatchedItemsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(WarehouseErrorCodes.WarehouseForbidden);
        await unitOfWork.WarehouseItems.DidNotReceive()
            .GetUnbatchedWithOrderAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUnbatchedItems_admin_returns_list_from_repository()
    {
        Guid orderId = Guid.NewGuid();
        var row = new WarehouseItemListRow(
            Guid.NewGuid(),
            orderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            WarehouseQcStatus.Passed,
            null,
            BatchId: null,
            WarehouseItemCreatedAt: DateTimeOffset.UtcNow,
            OrderStatus.QcPassed,
            PaymentStatus.Paid,
            OrderTotalAmount: 10m,
            OrderTotalCurrency: Currency.USD,
            OrderCreatedAt: DateTimeOffset.UtcNow.AddDays(-1));

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var warehouseItems = Substitute.For<IWarehouseItemRepository>();
        unitOfWork.WarehouseItems.Returns(warehouseItems);
        warehouseItems.GetUnbatchedWithOrderAsync(Arg.Any<CancellationToken>())
            .Returns(new List<WarehouseItemListRow> { row });

        var handler = new GetUnbatchedItemsQueryHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            NullLogger<GetUnbatchedItemsQueryHandler>.Instance);

        var result = await handler.Handle(new GetUnbatchedItemsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle();
        result.Value[0].OrderId.Should().Be(orderId);
        result.Value[0].QcStatus.Should().Be(WarehouseQcStatus.Passed);
        result.Value[0].BatchId.Should().BeNull();
        await warehouseItems.Received(1).GetUnbatchedWithOrderAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateQcStatus_non_admin_returns_WAREHOUSE_FORBIDDEN()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new UpdateQcStatusCommandHandler(
            unitOfWork,
            CreateCustomerCurrentUser(),
            NullLogger<UpdateQcStatusCommandHandler>.Instance);

        var result = await handler.Handle(
            new UpdateQcStatusCommand(Guid.NewGuid(), WarehouseQcStatus.Passed, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(WarehouseErrorCodes.WarehouseForbidden);
        await unitOfWork.WarehouseItems.DidNotReceive()
            .GetByIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateQcStatus_failed_updates_item_order_stays_at_warehouse()
    {
        Guid orderId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        Order order = CreateSingleLineAtWarehouseOrder(orderId, productId, Guid.NewGuid());
        Guid whId = Guid.NewGuid();
        WarehouseItem wh = CreatePendingWarehouseItem(whId, orderId, productId);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orders = Substitute.For<IOrderRepository>();
        var warehouseItems = Substitute.For<IWarehouseItemRepository>();
        unitOfWork.Orders.Returns(orders);
        unitOfWork.WarehouseItems.Returns(warehouseItems);
        warehouseItems.GetByIdForUpdateAsync(whId, Arg.Any<CancellationToken>()).Returns(wh);
        orders.GetByIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new UpdateQcStatusCommandHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            NullLogger<UpdateQcStatusCommandHandler>.Instance);

        var result = await handler.Handle(
            new UpdateQcStatusCommand(whId, WarehouseQcStatus.Failed, "Scratch on unit"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.AtWarehouse);
        wh.QcStatus.Should().Be(WarehouseQcStatus.Failed);
        wh.QcNotes.Should().Be("Scratch on unit");
        await warehouseItems.Received(1).UpdateAsync(wh, Arg.Any<CancellationToken>());
        await orders.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
        await warehouseItems.DidNotReceive()
            .GetByOrderIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateQcStatus_when_all_lines_passed_transitions_order_to_QcPassed()
    {
        Guid orderId = Guid.NewGuid();
        Guid p1 = Guid.NewGuid();
        Guid p2 = Guid.NewGuid();
        Order order = CreateTwoLineAtWarehouseOrder(orderId, p1, p2, Guid.NewGuid());
        Guid wh1 = Guid.NewGuid();
        Guid wh2 = Guid.NewGuid();
        WarehouseItem item1 = CreatePendingWarehouseItem(wh1, orderId, p1);
        WarehouseItem item2 = CreatePendingWarehouseItem(wh2, orderId, p2);
        var tracked = new List<WarehouseItem> { item1, item2 };

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orders = Substitute.For<IOrderRepository>();
        var warehouseItems = Substitute.For<IWarehouseItemRepository>();
        unitOfWork.Orders.Returns(orders);
        unitOfWork.WarehouseItems.Returns(warehouseItems);
        orders.GetByIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        warehouseItems.GetByOrderIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(tracked);
        warehouseItems.GetByIdForUpdateAsync(wh1, Arg.Any<CancellationToken>()).Returns(item1);
        warehouseItems.GetByIdForUpdateAsync(wh2, Arg.Any<CancellationToken>()).Returns(item2);

        var handler = new UpdateQcStatusCommandHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            NullLogger<UpdateQcStatusCommandHandler>.Instance);

        (await handler.Handle(new UpdateQcStatusCommand(wh1, WarehouseQcStatus.Passed, null), CancellationToken.None))
            .IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.AtWarehouse);

        (await handler.Handle(new UpdateQcStatusCommand(wh2, WarehouseQcStatus.Passed, null), CancellationToken.None))
            .IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.QcPassed);
    }

    [Fact]
    public async Task UpdateQcStatus_after_passed_returns_WAREHOUSE_QC_INVALID()
    {
        Guid orderId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        Order order = CreateSingleLineAtWarehouseOrder(orderId, productId, Guid.NewGuid());
        Guid whId = Guid.NewGuid();
        WarehouseItem wh = CreatePendingWarehouseItem(whId, orderId, productId);
        wh.ApplyQcOutcome(WarehouseQcStatus.Passed, false, null);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orders = Substitute.For<IOrderRepository>();
        var warehouseItems = Substitute.For<IWarehouseItemRepository>();
        unitOfWork.Orders.Returns(orders);
        unitOfWork.WarehouseItems.Returns(warehouseItems);
        warehouseItems.GetByIdForUpdateAsync(whId, Arg.Any<CancellationToken>()).Returns(wh);
        orders.GetByIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new UpdateQcStatusCommandHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            NullLogger<UpdateQcStatusCommandHandler>.Instance);

        var result = await handler.Handle(
            new UpdateQcStatusCommand(whId, WarehouseQcStatus.Failed, "too late"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(WarehouseErrorCodes.WarehouseQcInvalid);
    }

    private static WarehouseItem CreatePendingWarehouseItem(Guid id, Guid orderId, Guid productId)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return WarehouseItem.Create(id, orderId, productId, now, now, now).Value!;
    }

    private static Order CreateSingleLineAtWarehouseOrder(Guid orderId, Guid productId, Guid customerId)
    {
        var order = CreateSingleLinePaidOrder(orderId, productId, customerId);
        order.MarkArrivedAtWarehouse(Guid.NewGuid());
        order.PopDomainEvents();
        return order;
    }

    private static Order CreateTwoLineAtWarehouseOrder(Guid orderId, Guid productId1, Guid productId2, Guid customerId)
    {
        var price = Money.Create(5m, Currency.USD).Value!;
        var line1 = OrderItem.Create(productId1, "A", price, quantity: 1).Value!;
        var line2 = OrderItem.Create(productId2, "B", price, quantity: 1).Value!;
        var order = Order.Create(
            orderId,
            customerId,
            [line1, line2],
            TestDeliveryAddress,
            Money.Create(10m, Currency.USD).Value!,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;
        order.ConfirmPayment("ref");
        order.MarkArrivedAtWarehouse(Guid.NewGuid());
        order.PopDomainEvents();
        return order;
    }

    private static Order CreateOrderInStatus(Guid orderId, OrderStatus status)
    {
        var price = Money.Create(10m, Currency.USD).Value!;
        var orderItem = OrderItem.Create(Guid.NewGuid(), "Line", price, quantity: 1).Value!;
        var order = Order.Create(
            orderId,
            Guid.NewGuid(),
            [orderItem],
            TestDeliveryAddress,
            Money.Create(10m, Currency.USD).Value!,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;
        if (status == OrderStatus.Paid)
            order.ConfirmPayment("ref");
        return order;
    }

    private static Order CreateSingleLinePaidOrder(Guid orderId, Guid productId, Guid customerId)
    {
        var price = Money.Create(10m, Currency.USD).Value!;
        var orderItem = OrderItem.Create(productId, "Line", price, quantity: 1).Value!;
        var order = Order.Create(
            orderId,
            customerId,
            [orderItem],
            TestDeliveryAddress,
            Money.Create(10m, Currency.USD).Value!,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;
        order.ConfirmPayment("ref");
        return order;
    }

    private static ICurrentUser CreateCustomerCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Customer);
        return currentUser;
    }

    private static ICurrentUser CreateAdminCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);
        return currentUser;
    }
}
