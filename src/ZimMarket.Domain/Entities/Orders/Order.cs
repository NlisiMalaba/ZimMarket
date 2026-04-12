using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.Extensions;
using ZimMarket.Domain.ValueObjects;
using ZimMarket.Shared;

namespace ZimMarket.Domain.Entities.Orders;

public sealed class Order : BaseEntity
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    public Guid CustomerId { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items;

    public Address DeliveryAddress { get; private set; } = null!;

    public OrderStatus Status { get; private set; }

    public PaymentStatus PaymentStatus { get; private set; }

    public string? PaymentReference { get; private set; }

    public Money TotalAmount { get; private set; } = null!;

    public string? CancellationReason { get; private set; }

    public static Result<Order> Create(
        Guid id,
        Guid customerId,
        IReadOnlyList<OrderItem> items,
        Address deliveryAddress,
        Money totalAmount,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (customerId == Guid.Empty)
            return Result<Order>.Failure("Customer id is required.");

        ArgumentNullException.ThrowIfNull(deliveryAddress);
        ArgumentNullException.ThrowIfNull(totalAmount);

        if (items.Count == 0)
            return Result<Order>.Failure("Order must contain at least one line.");

        foreach (var item in items)
        {
            if (item.UnitPrice.Currency != totalAmount.Currency)
                return Result<Order>.Failure("All order lines must use the same currency as the order total.");
        }

        var sum = SumItems(items);
        if (sum.Amount != totalAmount.Amount)
            return Result<Order>.Failure("Total amount must equal the sum of line totals.");

        var order = new Order
        {
            Id = id,
            CustomerId = customerId,
            DeliveryAddress = deliveryAddress,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            PaymentReference = null,
            TotalAmount = totalAmount,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        order._items.AddRange(items);

        return Result<Order>.Success(order);
    }

    public void ConfirmPayment(string reference)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Payment can only be confirmed while the order is pending.");

        if (string.IsNullOrWhiteSpace(reference))
            throw new DomainException("Payment reference is required.");

        var trimmed = reference.Trim();
        PaymentReference = trimmed;
        PaymentStatus = PaymentStatus.Paid;
        Status = OrderStatus.Paid;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new PaymentConfirmedEvent(Id, trimmed));
    }

    public void Cancel(string reason)
    {
        if (Status.IsTerminal())
            throw new DomainException("Cannot cancel an order that is already in a terminal state.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Cancellation reason is required.");

        if (!Status.CanTransitionTo(OrderStatus.Cancelled))
            throw new DomainException($"Cannot cancel an order in status {Status}.");

        CancellationReason = reason.Trim();
        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(OrderStatus next)
    {
        if (!Status.CanTransitionTo(next))
            throw new DomainException($"Cannot transition order status from {Status} to {next}.");

        Status = next;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static Money SumItems(IReadOnlyList<OrderItem> items)
    {
        var currency = items[0].UnitPrice.Currency;
        decimal sum = 0;
        foreach (var item in items)
        {
            sum += item.LineTotal.Amount;
        }

        var result = Money.Create(decimal.Round(sum, 2, MidpointRounding.AwayFromZero), currency);
        if (result.IsFailure)
            throw new InvalidOperationException(string.Join("; ", result.Errors));

        return result.Value!;
    }
}
