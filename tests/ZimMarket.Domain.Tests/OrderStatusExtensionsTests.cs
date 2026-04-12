using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Extensions;
using FluentAssertions;

namespace ZimMarket.Domain.Tests;

public class OrderStatusExtensionsTests
{
    public static TheoryData<OrderStatus, OrderStatus, bool> AllTransitionExpectations()
    {
        var data = new TheoryData<OrderStatus, OrderStatus, bool>();
        foreach (var current in Enum.GetValues<OrderStatus>())
        {
            foreach (var next in Enum.GetValues<OrderStatus>())
            {
                data.Add(current, next, ExpectedCanTransition(current, next));
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllTransitionExpectations))]
    public void CanTransitionTo_follows_domain_policy(OrderStatus current, OrderStatus next, bool expected)
    {
        current.CanTransitionTo(next).Should().Be(expected);
    }

    private static bool ExpectedCanTransition(OrderStatus current, OrderStatus next)
    {
        if (current == next)
            return true;

        if (current is OrderStatus.Delivered or OrderStatus.Cancelled or OrderStatus.Refunded)
            return false;

        return Allowed.TryGetValue(current, out var set) && set.Contains(next);
    }

    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> Allowed = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.AtWarehouse, OrderStatus.Cancelled, OrderStatus.Refunded],
        [OrderStatus.AtWarehouse] = [OrderStatus.QcPassed, OrderStatus.Cancelled, OrderStatus.Refunded],
        [OrderStatus.QcPassed] = [OrderStatus.Batched, OrderStatus.Cancelled, OrderStatus.Refunded],
        [OrderStatus.Batched] = [OrderStatus.OutForDelivery, OrderStatus.Cancelled, OrderStatus.Refunded],
        [OrderStatus.OutForDelivery] = [OrderStatus.Delivered, OrderStatus.Cancelled, OrderStatus.Refunded],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = [],
        [OrderStatus.Refunded] = []
    };
}
