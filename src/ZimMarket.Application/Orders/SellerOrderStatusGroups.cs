using ZimMarket.Domain.Enums;

namespace ZimMarket.Application.Orders;

internal static class SellerOrderStatusGroups
{
    public static IReadOnlyList<OrderStatus>? ResolveStatuses(SellerOrderStatusGroup? group) =>
        group switch
        {
            null => null,
            SellerOrderStatusGroup.Completed => [OrderStatus.Delivered],
            SellerOrderStatusGroup.Processing =>
            [
                OrderStatus.Paid,
                OrderStatus.AtWarehouse,
                OrderStatus.QcPassed,
                OrderStatus.Batched,
                OrderStatus.OutForDelivery
            ],
            SellerOrderStatusGroup.Pending => [OrderStatus.Pending],
            SellerOrderStatusGroup.Cancelled => [OrderStatus.Cancelled],
            _ => null
        };
}
