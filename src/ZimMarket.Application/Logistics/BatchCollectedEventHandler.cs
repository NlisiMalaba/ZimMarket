using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Logistics;

/// <summary>
/// Notifies customers when their driver's batch has been collected for delivery.
/// </summary>
public sealed class BatchCollectedEventHandler : INotificationHandler<BatchCollectedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationJobScheduler _jobs;
    private readonly ILogger<BatchCollectedEventHandler> _logger;

    public BatchCollectedEventHandler(
        IUnitOfWork unitOfWork,
        INotificationJobScheduler jobs,
        ILogger<BatchCollectedEventHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(BatchCollectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var batch = await _unitOfWork.DeliveryBatches
                .GetByIdAsync(notification.BatchId, cancellationToken)
                .ConfigureAwait(false);

            if (batch is null)
            {
                _logger.LogWarning(
                    "BatchCollectedEvent for missing batch {BatchId}; customer pushes skipped.",
                    notification.BatchId);
                return;
            }

            foreach (Guid orderId in batch.OrderIds)
            {
                var order = await _unitOfWork.Orders
                    .GetByIdAsync(orderId, cancellationToken)
                    .ConfigureAwait(false);

                if (order is null)
                {
                    _logger.LogWarning(
                        "BatchCollectedEvent batch {BatchId} references missing order {OrderId}.",
                        notification.BatchId,
                        orderId);
                    continue;
                }

                Customer? customer = await _unitOfWork.Customers
                    .GetByIdAsync(order.CustomerId, cancellationToken)
                    .ConfigureAwait(false);

                if (customer is null)
                {
                    _logger.LogWarning(
                        "Order {OrderId} has unknown customer {CustomerId}; batch-collected push skipped.",
                        order.Id,
                        order.CustomerId);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(customer.PushNotificationToken))
                    continue;

                _jobs.EnqueuePushToToken(
                    customer.PushNotificationToken,
                    "Your order is on the way!",
                    $"Your order {order.Id:D} is on the way.",
                    new Dictionary<string, string>
                    {
                        ["orderId"] = order.Id.ToString("D"),
                        ["batchId"] = notification.BatchId.ToString("D"),
                        ["event"] = "batch_collected"
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to dispatch batch-collected notifications for batch {BatchId}. Batch remains committed.",
                notification.BatchId);
        }
    }
}
