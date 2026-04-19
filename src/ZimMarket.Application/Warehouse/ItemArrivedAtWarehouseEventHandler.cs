using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Warehouse;

public sealed class ItemArrivedAtWarehouseEventHandler : INotificationHandler<ItemArrivedAtWarehouseEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationJobScheduler _jobs;
    private readonly ILogger<ItemArrivedAtWarehouseEventHandler> _logger;

    public ItemArrivedAtWarehouseEventHandler(
        IUnitOfWork unitOfWork,
        INotificationJobScheduler jobs,
        ILogger<ItemArrivedAtWarehouseEventHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(ItemArrivedAtWarehouseEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _unitOfWork.Orders
                .GetByIdAsync(notification.OrderId, cancellationToken)
                .ConfigureAwait(false);

            if (order is null)
            {
                _logger.LogWarning(
                    "ItemArrivedAtWarehouseEvent for missing order {OrderId}.",
                    notification.OrderId);
                return;
            }

            Customer? customer = await _unitOfWork.Customers
                .GetByIdAsync(order.CustomerId, cancellationToken)
                .ConfigureAwait(false);

            if (customer is null)
            {
                _logger.LogWarning(
                    "Order {OrderId} has unknown customer {CustomerId}; item-arrival push skipped.",
                    order.Id,
                    order.CustomerId);
                return;
            }

            if (string.IsNullOrWhiteSpace(customer.PushNotificationToken))
                return;

            _jobs.EnqueuePushToToken(
                customer.PushNotificationToken,
                "Order at warehouse",
                $"Your order {order.Id:D} has arrived at our warehouse.",
                new Dictionary<string, string>
                {
                    ["orderId"] = order.Id.ToString("D"),
                    ["warehouseItemId"] = notification.WarehouseItemId.ToString("D"),
                    ["event"] = "item_arrived_at_warehouse"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to dispatch item-arrived-at-warehouse notification for order {OrderId}.",
                notification.OrderId);
        }
    }
}
