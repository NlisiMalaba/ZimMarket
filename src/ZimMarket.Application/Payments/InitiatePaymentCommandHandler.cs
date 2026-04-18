using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Payments;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Payments;

public sealed class InitiatePaymentCommandHandler : IRequestHandler<InitiatePaymentCommand, Result<PaymentInitiateDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IPaymentGatewayFactory _paymentGatewayFactory;
    private readonly ILogger<InitiatePaymentCommandHandler> _logger;

    public InitiatePaymentCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IPaymentGatewayFactory paymentGatewayFactory,
        ILogger<InitiatePaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _paymentGatewayFactory = paymentGatewayFactory ?? throw new ArgumentNullException(nameof(paymentGatewayFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PaymentInitiateDto>> Handle(
        InitiatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
        {
            return Result<PaymentInitiateDto>.Failure(
                PaymentErrorCodes.Unauthorized,
                "Authentication is required.");
        }

        if (_currentUser.Role != UserRole.Customer)
        {
            return Result<PaymentInitiateDto>.Failure(
                PaymentErrorCodes.CustomerRoleRequired,
                "Only customers can initiate payment for an order.");
        }

        string idempotencyKey = request.IdempotencyKey.Trim();

        var existing = await _unitOfWork.PaymentIdempotency
            .GetByKeyAsync(idempotencyKey, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            if (existing.OrderId != request.OrderId)
            {
                return Result<PaymentInitiateDto>.Failure(
                    PaymentErrorCodes.IdempotencyKeyConflict,
                    "This idempotency key was already used for a different order.");
            }

            if (existing.CustomerId != _currentUser.UserId)
            {
                return Result<PaymentInitiateDto>.Failure(
                    PaymentErrorCodes.Forbidden,
                    "You cannot access this payment initiation.");
            }

            return Result<PaymentInitiateDto>.Success(
                new PaymentInitiateDto
                {
                    PaymentUrl = existing.PaymentUrl,
                    GatewayReference = existing.GatewayReference
                });
        }

        var order = await _unitOfWork.Orders
            .GetByIdTrackedAsync(request.OrderId, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
        {
            return Result<PaymentInitiateDto>.Failure(
                PaymentErrorCodes.OrderNotFound,
                "Order was not found.");
        }

        if (order.CustomerId != _currentUser.UserId)
        {
            return Result<PaymentInitiateDto>.Failure(
                PaymentErrorCodes.Forbidden,
                "You do not have access to this order.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            return Result<PaymentInitiateDto>.Failure(
                PaymentErrorCodes.OrderNotPending,
                "Payment can only be started for a pending order.");
        }

        if (order.PaymentStatus is not PaymentStatus.Pending and not PaymentStatus.Failed)
        {
            return Result<PaymentInitiateDto>.Failure(
                PaymentErrorCodes.PaymentAlreadyInitiated,
                "Payment has already been initiated or completed for this order.");
        }

        IPaymentGateway gateway;
        try
        {
            gateway = _paymentGatewayFactory.Create(request.PaymentMethod);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Unsupported payment method {Method}.", request.PaymentMethod);
            return Result<PaymentInitiateDto>.Failure(
                PaymentErrorCodes.MethodNotSupported,
                "The selected payment method is not available.");
        }

        var paymentRequest = new PaymentRequest
        {
            OrderId = order.Id,
            Amount = order.TotalAmount.Amount,
            Currency = order.TotalAmount.Currency.ToString(),
            Description = $"Order {order.Id:N}"
        };

        PaymentInitiateResult gatewayResult = await gateway
            .InitiateAsync(paymentRequest, cancellationToken)
            .ConfigureAwait(false);

        if (!gatewayResult.Success)
        {
            string message = string.IsNullOrWhiteSpace(gatewayResult.ErrorMessage)
                ? "The payment provider did not accept this request."
                : gatewayResult.ErrorMessage.Trim();

            _logger.LogWarning(
                "Payment initiation failed for order {OrderId}: {ErrorCode} — {ErrorMessage}.",
                order.Id,
                gatewayResult.ErrorCode,
                message);

            string code = string.IsNullOrWhiteSpace(gatewayResult.ErrorCode)
                ? PaymentErrorCodes.GatewayRejected
                : gatewayResult.ErrorCode.Trim();

            return Result<PaymentInitiateDto>.Failure(code, message);
        }

        string? paymentUrl = gatewayResult.RedirectUrl?.Trim();
        if (string.IsNullOrWhiteSpace(paymentUrl))
        {
            return Result<PaymentInitiateDto>.Failure(
                PaymentErrorCodes.MissingCheckoutUrl,
                "The payment provider did not return a checkout URL.");
        }

        string gatewayReference = (gatewayResult.ExternalPaymentId ?? gatewayResult.PollUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(gatewayReference))
            gatewayReference = order.Id.ToString("N");

        try
        {
            order.MarkPaymentInitiated(gatewayReference, request.PaymentMethod);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Invalid payment initiation state for order {OrderId}.", order.Id);
            return Result<PaymentInitiateDto>.Failure(PaymentErrorCodes.InvalidState, ex.Message);
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var idempotencyRecord = PaymentIdempotencyRecord.Create(
            Guid.NewGuid(),
            idempotencyKey,
            order.Id,
            order.CustomerId,
            gatewayReference,
            paymentUrl,
            request.PaymentMethod,
            now,
            now);

        await _unitOfWork.PaymentIdempotency.AddAsync(idempotencyRecord, cancellationToken).ConfigureAwait(false);

        return Result<PaymentInitiateDto>.Success(
            new PaymentInitiateDto
            {
                PaymentUrl = paymentUrl,
                GatewayReference = gatewayReference
            });
    }
}
