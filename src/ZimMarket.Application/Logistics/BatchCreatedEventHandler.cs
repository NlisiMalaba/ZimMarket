using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Logistics;

/// <summary>
/// Notifies the assigned driver of a new pickup batch (push when a device token is registered, plus SMS and email).
/// </summary>
public sealed class BatchCreatedEventHandler : INotificationHandler<BatchCreatedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationJobScheduler _jobs;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<BatchCreatedEventHandler> _logger;

    public BatchCreatedEventHandler(
        IUnitOfWork unitOfWork,
        INotificationJobScheduler jobs,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<BatchCreatedEventHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(BatchCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            Driver? driver = await _unitOfWork.Drivers
                .GetByIdAsync(notification.DriverId, cancellationToken)
                .ConfigureAwait(false);

            if (driver is null)
            {
                _logger.LogWarning(
                    "BatchCreatedEvent for unknown driver {DriverId}; notifications skipped.",
                    notification.DriverId);
                return;
            }

            int orderCount = notification.OrderIds.Count;
            string orderList = string.Join(", ", notification.OrderIds.Select(x => x.ToString("D")));
            string pickupSummary =
                $"Pickup warehouse id: {notification.PickupWarehouseId:D}. " +
                $"Orders ({orderCount}): {orderList}.";

            string smsBody =
                $"ZimMarket: New delivery batch {notification.BatchId:D}. {pickupSummary}";

            await _smsService
                .SendAsync(driver.PhoneNumber.Value, smsBody, cancellationToken)
                .ConfigureAwait(false);

            string emailBody =
                $"""
                Hello {driver.FullName},

                You have been assigned a new delivery batch on ZimMarket.

                Batch id: {notification.BatchId:D}
                {pickupSummary}

                Please proceed to the warehouse for collection when ready.

                — ZimMarket
                """;

            await _emailService
                .SendAsync(
                    new EmailMessage
                    {
                        To = driver.Email,
                        Subject = $"ZimMarket: New delivery batch {notification.BatchId:D}",
                        Body = emailBody,
                        IsHtml = false
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(driver.DeliveryPushNotificationToken))
            {
                _jobs.EnqueuePushToToken(
                    driver.DeliveryPushNotificationToken,
                    "New pickup batch",
                    $"Batch {notification.BatchId:D}. {pickupSummary}",
                    new Dictionary<string, string>
                    {
                        ["batchId"] = notification.BatchId.ToString("D"),
                        ["driverId"] = notification.DriverId.ToString("D"),
                        ["pickupWarehouseId"] = notification.PickupWarehouseId.ToString("D"),
                        ["event"] = "batch_created"
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to notify driver for batch {BatchId}. Batch remains committed.",
                notification.BatchId);
        }
    }
}
