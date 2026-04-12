using ZimMarket.Domain.Enums;

namespace ZimMarket.Domain.Extensions;

public static class OrderStatusExtensions
{
    private static readonly IReadOnlyDictionary<OrderStatus, HashSet<OrderStatus>> AllowedTransitions =
        new Dictionary<OrderStatus, HashSet<OrderStatus>>
        {
            [OrderStatus.Pending] =
            [
                OrderStatus.Paid,
                OrderStatus.Cancelled
            ],
            [OrderStatus.Paid] =
            [
                OrderStatus.AtWarehouse,
                OrderStatus.Cancelled,
                OrderStatus.Refunded
            ],
            [OrderStatus.AtWarehouse] =
            [
                OrderStatus.QcPassed,
                OrderStatus.Cancelled,
                OrderStatus.Refunded
            ],
            [OrderStatus.QcPassed] =
            [
                OrderStatus.Batched,
                OrderStatus.Cancelled,
                OrderStatus.Refunded
            ],
            [OrderStatus.Batched] =
            [
                OrderStatus.OutForDelivery,
                OrderStatus.Cancelled,
                OrderStatus.Refunded
            ],
            [OrderStatus.OutForDelivery] =
            [
                OrderStatus.Delivered,
                OrderStatus.Cancelled,
                OrderStatus.Refunded
            ],
            [OrderStatus.Delivered] = [],
            [OrderStatus.Cancelled] = [],
            [OrderStatus.Refunded] = []
        };

    public static bool IsTerminal(this OrderStatus status) =>
        status is OrderStatus.Delivered or OrderStatus.Cancelled or OrderStatus.Refunded;

    public static bool CanTransitionTo(this OrderStatus current, OrderStatus next)
    {
        if (current == next)
            return true;

        if (current.IsTerminal())
            return false;

        return AllowedTransitions.TryGetValue(current, out var allowed) && allowed.Contains(next);
    }
}
