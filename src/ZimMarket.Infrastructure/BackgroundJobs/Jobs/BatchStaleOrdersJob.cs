using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Infrastructure.Persistence;

namespace ZimMarket.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Flags unpaid orders older than the SLA for cancellation (periodic sweep).
/// </summary>
public sealed class BatchStaleOrdersJob
{
    private const string StaleCancellationReason = "Payment not completed within 2 hours";

    private readonly AppDbContext _dbContext;
    private readonly INotificationJobScheduler _notificationJobs;
    private readonly ILogger<BatchStaleOrdersJob> _logger;

    public BatchStaleOrdersJob(
        AppDbContext dbContext,
        INotificationJobScheduler notificationJobs,
        ILogger<BatchStaleOrdersJob> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _notificationJobs = notificationJobs ?? throw new ArgumentNullException(nameof(notificationJobs));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset cutoff = now.AddHours(-2);

        List<Domain.Entities.Orders.Order> staleOrders = await _dbContext.Orders
            .Include(x => x.Items)
            .Where(x =>
                x.Status == OrderStatus.Pending
                && x.PaymentStatus != PaymentStatus.Paid
                && x.CreatedAt < cutoff)
            .ToListAsync()
            .ConfigureAwait(false);

        if (staleOrders.Count == 0)
        {
            _logger.LogInformation("BatchStaleOrdersJob completed: no stale pending orders found.");
            return;
        }

        int cancelledCount = 0;

        foreach (Domain.Entities.Orders.Order order in staleOrders)
        {
            try
            {
                order.Cancel(StaleCancellationReason);

                foreach (var item in order.Items)
                {
                    var product = await _dbContext.Products
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId)
                        .ConfigureAwait(false);

                    if (product is null)
                    {
                        _logger.LogWarning(
                            "Stale-order cancellation stock restore skipped: order {OrderId}, product {ProductId} not found.",
                            order.Id,
                            item.ProductId);
                        continue;
                    }

                    product.UpdateStock(item.Quantity);
                }

                await NotifyCustomerAsync(order).ConfigureAwait(false);
                cancelledCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel stale order {OrderId}.", order.Id);
            }
        }

        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation(
            "BatchStaleOrdersJob completed: cancelled {CancelledCount} stale orders (cutoff={CutoffUtc}).",
            cancelledCount,
            cutoff);
    }

    private async Task NotifyCustomerAsync(Domain.Entities.Orders.Order order)
    {
        Customer? customer = await _dbContext.Set<Customer>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == order.CustomerId)
            .ConfigureAwait(false);

        if (customer is null)
        {
            _logger.LogWarning("Cancelled stale order {OrderId} has missing customer {CustomerId}.", order.Id, order.CustomerId);
            return;
        }

        string smsBody = $"ZimMarket: order {order.Id:D} was cancelled because payment was not completed within 2 hours.";
        _notificationJobs.EnqueueSms(customer.PhoneNumber.Value, smsBody);

        if (!string.IsNullOrWhiteSpace(customer.PushNotificationToken))
        {
            _notificationJobs.EnqueuePushToToken(
                customer.PushNotificationToken,
                "Order cancelled",
                "Your order was cancelled because payment was not completed within 2 hours.",
                new Dictionary<string, string>
                {
                    ["orderId"] = order.Id.ToString("D"),
                    ["event"] = "order_cancelled_stale"
                });
        }

        _notificationJobs.EnqueueEmail(new EmailMessage
        {
            To = customer.Email,
            Subject = "ZimMarket: Order cancelled due to unpaid timeout",
            Body =
                $"""
                Hello {customer.FullName},

                Your order {order.Id:D} has been cancelled because payment was not completed within 2 hours.

                You can place a new order at any time.

                - ZimMarket
                """,
            IsHtml = false
        });
    }
}
