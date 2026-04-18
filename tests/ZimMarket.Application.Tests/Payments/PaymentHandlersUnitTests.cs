using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Application.Payments;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Entities.Payments;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Payments;

public sealed class PaymentHandlersUnitTests
{
    [Fact]
    public async Task InitiatePayment_order_not_owned_by_caller_returns_forbidden()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var orderRepo = Substitute.For<IOrderRepository>();
        var idempotencyRepo = Substitute.For<IPaymentIdempotencyRepository>();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Customer);

        var order = CreatePendingOrder(customerId: Guid.NewGuid());
        orderRepo.GetByIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        idempotencyRepo.GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PaymentIdempotencyRecord?)null);

        unitOfWork.Orders.Returns(orderRepo);
        unitOfWork.PaymentIdempotency.Returns(idempotencyRepo);

        var gatewayFactory = Substitute.For<IPaymentGatewayFactory>();
        var handler = new InitiatePaymentCommandHandler(
            unitOfWork,
            currentUser,
            gatewayFactory,
            NullLogger<InitiatePaymentCommandHandler>.Instance);

        var result = await handler.Handle(
            new InitiatePaymentCommand(order.Id, PaymentMethod.Paynow, "idem-1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PaymentErrorCodes.Forbidden);
        gatewayFactory.DidNotReceive().Create(Arg.Any<PaymentMethod>());
    }

    [Fact]
    public async Task InitiatePayment_existing_idempotency_key_returns_same_result_without_duplicate()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var orderRepo = Substitute.For<IOrderRepository>();
        var idempotencyRepo = Substitute.For<IPaymentIdempotencyRepository>();

        Guid customerId = Guid.NewGuid();
        Guid orderId = Guid.NewGuid();
        const string key = "idem-same";

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(customerId);
        currentUser.Role.Returns(UserRole.Customer);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var existing = PaymentIdempotencyRecord.Create(
            Guid.NewGuid(),
            key,
            orderId,
            customerId,
            "gateway-ref-1",
            "https://pay.test/checkout/1",
            PaymentMethod.Paynow,
            now,
            now);

        idempotencyRepo.GetByKeyAsync(key, Arg.Any<CancellationToken>()).Returns(existing);
        unitOfWork.Orders.Returns(orderRepo);
        unitOfWork.PaymentIdempotency.Returns(idempotencyRepo);

        var gatewayFactory = Substitute.For<IPaymentGatewayFactory>();
        var handler = new InitiatePaymentCommandHandler(
            unitOfWork,
            currentUser,
            gatewayFactory,
            NullLogger<InitiatePaymentCommandHandler>.Instance);

        var result = await handler.Handle(
            new InitiatePaymentCommand(orderId, PaymentMethod.Paynow, key),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentUrl.Should().Be(existing.PaymentUrl);
        result.Value.GatewayReference.Should().Be(existing.GatewayReference);

        gatewayFactory.DidNotReceive().Create(Arg.Any<PaymentMethod>());
        await orderRepo.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await idempotencyRepo.DidNotReceive().AddAsync(Arg.Any<PaymentIdempotencyRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessWebhook_invalid_hmac_returns_failure_without_processing()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orderRepo = Substitute.For<IOrderRepository>();
        unitOfWork.Orders.Returns(orderRepo);

        var gatewayFactory = Substitute.For<IPaymentGatewayFactory>();
        var gateway = Substitute.For<IPaymentGateway>();
        gatewayFactory.Create(PaymentMethod.Paynow).Returns(gateway);
        gateway.VerifyWebhookAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentWebhookResult
            {
                IsValid = false,
                ErrorMessage = "Invalid signature."
            });

        var handler = new ProcessPaymentWebhookCommandHandler(
            unitOfWork,
            gatewayFactory,
            NullLogger<ProcessPaymentWebhookCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessPaymentWebhookCommand("payload", "bad-hash", PaymentGatewayType.Paynow),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PaymentErrorCodes.WebhookInvalidSignature);
        await orderRepo.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessWebhook_duplicate_reference_is_idempotent()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orderRepo = Substitute.For<IOrderRepository>();
        unitOfWork.Orders.Returns(orderRepo);

        var gatewayFactory = Substitute.For<IPaymentGatewayFactory>();
        var gateway = Substitute.For<IPaymentGateway>();
        gatewayFactory.Create(PaymentMethod.Paynow).Returns(gateway);

        var order = CreatePendingOrder(Guid.NewGuid());
        order.ConfirmPayment("pay-ref-1");
        order.PopDomainEvents(); // clear initial event so we can validate idempotent re-processing.

        gateway.VerifyWebhookAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentWebhookResult
            {
                IsValid = true,
                OrderId = order.Id,
                PaymentReference = "pay-ref-1",
                Status = "Paid"
            });
        orderRepo.GetByIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new ProcessPaymentWebhookCommandHandler(
            unitOfWork,
            gatewayFactory,
            NullLogger<ProcessPaymentWebhookCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessPaymentWebhookCommand("payload", "good-hash", PaymentGatewayType.Paynow),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.PaymentStatus.Should().Be(PaymentStatus.Paid);
        order.PaymentReference.Should().Be("pay-ref-1");
        order.PopDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessWebhook_successful_payment_triggers_payment_confirmed_event()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var orderRepo = Substitute.For<IOrderRepository>();
        unitOfWork.Orders.Returns(orderRepo);

        var gatewayFactory = Substitute.For<IPaymentGatewayFactory>();
        var gateway = Substitute.For<IPaymentGateway>();
        gatewayFactory.Create(PaymentMethod.Paynow).Returns(gateway);

        var order = CreatePendingOrder(Guid.NewGuid());
        orderRepo.GetByIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        gateway.VerifyWebhookAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentWebhookResult
            {
                IsValid = true,
                OrderId = order.Id,
                PaymentReference = "pay-ref-success",
                Status = "Paid"
            });

        var handler = new ProcessPaymentWebhookCommandHandler(
            unitOfWork,
            gatewayFactory,
            NullLogger<ProcessPaymentWebhookCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessPaymentWebhookCommand("payload", "good-hash", PaymentGatewayType.Paynow),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaymentStatus.Should().Be(PaymentStatus.Paid);
        order.PaymentReference.Should().Be("pay-ref-success");

        var domainEvents = order.PopDomainEvents();
        domainEvents.Should().ContainSingle(e => e.GetType() == typeof(PaymentConfirmedEvent));
        domainEvents.OfType<PaymentConfirmedEvent>().Single().Reference.Should().Be("pay-ref-success");
    }

    private static Order CreatePendingOrder(Guid customerId)
    {
        Money total = Money.Create(50m, Currency.USD).Value!;
        Address address = Address.Create("12 Main St", "CBD", "Harare", "Zimbabwe").Value!;
        OrderItem item = OrderItem.Create(
            Guid.NewGuid(),
            "Product A",
            Money.Create(50m, Currency.USD).Value!,
            1).Value!;

        return Order.Create(
                Guid.NewGuid(),
                customerId,
                [item],
                address,
                total,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow)
            .Value!;
    }
}
