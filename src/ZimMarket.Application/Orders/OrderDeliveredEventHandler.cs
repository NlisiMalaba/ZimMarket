using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Orders;

/// <summary>
/// Sends delivery confirmation push and email receipt to the customer.
/// </summary>
public sealed class OrderDeliveredEventHandler : INotificationHandler<OrderDeliveredEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationJobScheduler _jobs;
    private readonly IEmailService _emailService;
    private readonly ILogger<OrderDeliveredEventHandler> _logger;

    public OrderDeliveredEventHandler(
        IUnitOfWork unitOfWork,
        INotificationJobScheduler jobs,
        IEmailService emailService,
        ILogger<OrderDeliveredEventHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(OrderDeliveredEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            Customer? customer = await _unitOfWork.Customers
                .GetByIdAsync(notification.CustomerId, cancellationToken)
                .ConfigureAwait(false);

            if (customer is null)
            {
                _logger.LogWarning(
                    "OrderDeliveredEvent for unknown customer {CustomerId}; notifications skipped.",
                    notification.CustomerId);
                return;
            }

            string currencyName = notification.TotalCurrency.ToString();
            string receiptBody =
                $"""
                Hello {customer.FullName},

                Your order {notification.OrderId:D} has been delivered.

                Total paid: {notification.TotalAmount:F2} {currencyName}

                Thank you for shopping on ZimMarket.

                — ZimMarket
                """;

            await _emailService
                .SendAsync(
                    new EmailMessage
                    {
                        To = customer.Email,
                        Subject = $"ZimMarket: Order {notification.OrderId:D} delivered",
                        Body = receiptBody,
                        IsHtml = false
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(customer.PushNotificationToken))
            {
                _jobs.EnqueuePushToToken(
                    customer.PushNotificationToken,
                    "Order delivered",
                    $"Your order {notification.OrderId:D} has been delivered. Check your email for your receipt.",
                    new Dictionary<string, string>
                    {
                        ["orderId"] = notification.OrderId.ToString("D"),
                        ["deliveryPhotoKey"] = notification.DeliveryPhotoKey,
                        ["event"] = "order_delivered"
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send order-delivered notifications for order {OrderId}. Delivery remains committed.",
                notification.OrderId);
        }
    }
}
