using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Catalogue;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Payments;

/// <summary>
/// Dispatches post-payment notifications to sellers and customer via fire-and-forget background jobs.
/// </summary>
public sealed class PaymentConfirmedEventHandler : INotificationHandler<PaymentConfirmedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationJobScheduler _jobs;
    private readonly ILogger<PaymentConfirmedEventHandler> _logger;

    public PaymentConfirmedEventHandler(
        IUnitOfWork unitOfWork,
        INotificationJobScheduler jobs,
        ILogger<PaymentConfirmedEventHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(PaymentConfirmedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _unitOfWork.Orders
                .GetByIdAsync(notification.OrderId, cancellationToken)
                .ConfigureAwait(false);

            if (order is null)
            {
                _logger.LogWarning(
                    "PaymentConfirmedEvent for unknown order {OrderId}; notifications skipped.",
                    notification.OrderId);
                return;
            }

            if (order.Status != OrderStatus.Paid || order.PaymentStatus != PaymentStatus.Paid)
            {
                _logger.LogWarning(
                    "PaymentConfirmedEvent received before order {OrderId} reached Paid state. Status={Status}, PaymentStatus={PaymentStatus}.",
                    order.Id,
                    order.Status,
                    order.PaymentStatus);
            }

            await NotifySellersAsync(order, notification.Reference, cancellationToken).ConfigureAwait(false);
            await NotifyCustomerAsync(order, notification.Reference, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to dispatch payment-confirmed notifications for order {OrderId}.",
                notification.OrderId);
        }
    }

    private async Task NotifySellersAsync(
        Domain.Entities.Orders.Order order,
        string paymentReference,
        CancellationToken cancellationToken)
    {
        var sellerIds = new HashSet<Guid>();
        foreach (var item in order.Items)
        {
            Product? product = await _unitOfWork.Products
                .GetByIdAsync(item.ProductId, cancellationToken)
                .ConfigureAwait(false);

            if (product is null)
            {
                _logger.LogWarning(
                    "Order {OrderId} contains missing product {ProductId}; seller notification skipped for that item.",
                    order.Id,
                    item.ProductId);
                continue;
            }

            sellerIds.Add(product.SellerId);
        }

        foreach (Guid sellerId in sellerIds)
        {
            Seller? seller = await _unitOfWork.Sellers
                .GetByIdAsync(sellerId, cancellationToken)
                .ConfigureAwait(false);

            if (seller is null)
            {
                _logger.LogWarning(
                    "Order {OrderId} references missing seller {SellerId}; notification skipped.",
                    order.Id,
                    sellerId);
                continue;
            }

            var pushData = new Dictionary<string, string>
            {
                ["orderId"] = order.Id.ToString("D"),
                ["paymentReference"] = paymentReference,
                ["event"] = "payment_confirmed"
            };

            _jobs.EnqueuePushToTopic(
                $"seller:{seller.Id:D}",
                "Payment received",
                $"Order {order.Id:D} has been paid and is ready for fulfillment.",
                pushData);

            _jobs.EnqueueEmail(new EmailMessage
            {
                To = seller.Email,
                Subject = "ZimMarket: Order payment confirmed",
                Body =
                    $"""
                    Hello {seller.FullName},

                    Payment has been confirmed for order {order.Id:D}.
                    Reference: {paymentReference}

                    Please prepare the order for fulfillment.

                    - ZimMarket
                    """,
                IsHtml = false
            });
        }
    }

    private async Task NotifyCustomerAsync(
        Domain.Entities.Orders.Order order,
        string paymentReference,
        CancellationToken cancellationToken)
    {
        Customer? customer = await _unitOfWork.Customers
            .GetByIdAsync(order.CustomerId, cancellationToken)
            .ConfigureAwait(false);

        if (customer is null)
        {
            _logger.LogWarning(
                "Order {OrderId} has unknown customer {CustomerId}; customer notifications skipped.",
                order.Id,
                order.CustomerId);
            return;
        }

        if (!string.IsNullOrWhiteSpace(customer.PushNotificationToken))
        {
            _jobs.EnqueuePushToToken(
                customer.PushNotificationToken,
                "Payment confirmed",
                $"We received payment for order {order.Id:D}.",
                new Dictionary<string, string>
                {
                    ["orderId"] = order.Id.ToString("D"),
                    ["paymentReference"] = paymentReference,
                    ["event"] = "payment_confirmed"
                });
        }

        _jobs.EnqueueSms(
            customer.PhoneNumber.Value,
            $"ZimMarket receipt: payment confirmed for order {order.Id:D}. Ref: {paymentReference}.");
    }
}