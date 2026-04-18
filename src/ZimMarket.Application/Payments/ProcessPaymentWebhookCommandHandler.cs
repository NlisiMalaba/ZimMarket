using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Orders;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Payments;

public sealed class ProcessPaymentWebhookCommandHandler : IRequestHandler<ProcessPaymentWebhookCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGatewayFactory _paymentGatewayFactory;
    private readonly ILogger<ProcessPaymentWebhookCommandHandler> _logger;

    public ProcessPaymentWebhookCommandHandler(
        IUnitOfWork unitOfWork,
        IPaymentGatewayFactory paymentGatewayFactory,
        ILogger<ProcessPaymentWebhookCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _paymentGatewayFactory = paymentGatewayFactory ?? throw new ArgumentNullException(nameof(paymentGatewayFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        IPaymentGateway gateway;
        try
        {
            gateway = _paymentGatewayFactory.Create(ToPaymentMethod(request.GatewayType));
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Webhook gateway {Gateway} is not configured.", request.GatewayType);
            return Result.Failure(
                PaymentErrorCodes.WebhookGatewayUnavailable,
                "The payment gateway is not available.");
        }

        string signature = request.Signature?.Trim() ?? string.Empty;
        PaymentWebhookResult verified = await gateway
            .VerifyWebhookAsync(request.Payload, signature, cancellationToken)
            .ConfigureAwait(false);

        if (!verified.IsValid)
        {
            string message = string.IsNullOrWhiteSpace(verified.ErrorMessage)
                ? "Webhook signature verification failed."
                : verified.ErrorMessage.Trim();

            _logger.LogWarning(
                "Rejected payment webhook for gateway {Gateway}: {Message}",
                request.GatewayType,
                message);

            return Result.Failure(PaymentErrorCodes.WebhookInvalidSignature, message);
        }

        if (verified.OrderId is null || verified.OrderId == Guid.Empty)
        {
            _logger.LogWarning("Payment webhook verified but did not include a usable order id.");
            return Result.Failure(
                PaymentErrorCodes.WebhookInvalidPayload,
                "Webhook payload did not identify an order.");
        }

        Guid orderId = verified.OrderId.Value;

        var order = await _unitOfWork.Orders
            .GetByIdTrackedAsync(orderId, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
        {
            _logger.LogWarning("Payment webhook referenced unknown order {OrderId}.", orderId);
            return Result.Failure(PaymentErrorCodes.WebhookOrderNotFound, "Order was not found.");
        }

        string? providerRefRaw = verified.PaymentReference?.Trim();
        string? status = verified.Status?.Trim();

        if (request.GatewayType == PaymentGatewayType.Paynow)
        {
            if (PaynowWebhookStatus.IsPaid(status))
                return HandlePaidPath(order, providerRefRaw);

            if (PaynowWebhookStatus.IsFailed(status))
                return HandleFailedPath(order, providerRefRaw, status, request.GatewayType);

            _logger.LogInformation(
                "Ignoring Paynow webhook with non-terminal status {Status} for order {OrderId}.",
                status ?? "(null)",
                orderId);

            return Result.Success();
        }

        // Ecocash (and future gateways): treat unknown status model as neutral until a dedicated parser exists.
        if (string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            return HandlePaidPath(order, providerRefRaw);

        if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase))
            return HandleFailedPath(order, providerRefRaw, status, request.GatewayType);

        _logger.LogInformation(
            "Ignoring webhook with status {Status} for gateway {Gateway}, order {OrderId}.",
            status ?? "(null)",
            request.GatewayType,
            orderId);

        return Result.Success();
    }

    private Result HandlePaidPath(Order order, string? providerPaymentReference)
    {
        if (order.PaymentStatus == PaymentStatus.Paid || order.Status == OrderStatus.Paid)
            return Result.Success();

        if (string.IsNullOrWhiteSpace(providerPaymentReference))
        {
            return Result.Failure(
                PaymentErrorCodes.WebhookMissingProviderReference,
                "Webhook did not include a provider payment reference.");
        }

        string reference = providerPaymentReference.Trim();

        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogWarning(
                "Ignoring paid webhook for order {OrderId} in status {Status}.",
                order.Id,
                order.Status);

            return Result.Success();
        }

        try
        {
            order.ConfirmPayment(reference);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Cannot confirm payment for order {OrderId}.", order.Id);
            return Result.Failure(PaymentErrorCodes.WebhookInvalidOrderState, ex.Message);
        }

        return Result.Success();
    }

    private Result HandleFailedPath(
        Order order,
        string? providerPaymentReference,
        string? status,
        PaymentGatewayType gatewayType)
    {
        string failureRef = string.IsNullOrWhiteSpace(providerPaymentReference)
            ? $"{gatewayType}:{order.Id:N}:failed"
            : providerPaymentReference.Trim();

        if (order.PaymentStatus == PaymentStatus.Failed
            && string.Equals(order.FailedGatewayPaymentReference, failureRef, StringComparison.OrdinalIgnoreCase))
            return Result.Success();

        if (order.Status == OrderStatus.Paid || order.PaymentStatus == PaymentStatus.Paid)
            return Result.Success();

        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogWarning(
                "Ignoring failed payment webhook for order {OrderId} in status {OrderStatus}.",
                order.Id,
                order.Status);

            return Result.Success();
        }

        try
        {
            order.MarkPaymentFailed(failureRef, status);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Cannot record payment failure for order {OrderId}.", order.Id);
            return Result.Failure(PaymentErrorCodes.WebhookInvalidOrderState, ex.Message);
        }

        return Result.Success();
    }

    private static PaymentMethod ToPaymentMethod(PaymentGatewayType gatewayType) =>
        gatewayType switch
        {
            PaymentGatewayType.Paynow => PaymentMethod.Paynow,
            PaymentGatewayType.Ecocash => PaymentMethod.Ecocash,
            _ => throw new NotSupportedException($"Gateway {gatewayType} is not supported.")
        };
}
