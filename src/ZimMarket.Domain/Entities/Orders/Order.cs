using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Domain.Extensions;
using ZimMarket.Domain.ValueObjects;
using ZimMarket.Shared;

namespace ZimMarket.Domain.Entities.Orders;

public sealed class Order : BaseEntity
{
    public const int MaxAdminOverrideReasonLength = 2000;

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

    /// <summary>Gateway-specific reference returned when payment was initiated (e.g. Paynow poll URL).</summary>
    public string? PaymentGatewayReference { get; private set; }

    /// <summary>Payment channel used for the current initiation attempt.</summary>
    public PaymentMethod? InitiatedPaymentMethod { get; private set; }

    /// <summary>Provider reference from the last failed webhook (used for idempotent failure handling).</summary>
    public string? FailedGatewayPaymentReference { get; private set; }

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

    /// <summary>
    /// Records that the order was persisted after checkout (lines reserved); raises <see cref="OrderPlacedEvent"/> for downstream handlers.
    /// </summary>
    public void MarkPlaced()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Order can only be marked as placed while pending.");

        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new OrderPlacedEvent(Id, CustomerId, TotalAmount.Amount));
    }

    /// <summary>
    /// Records that the customer started checkout with the payment provider while the order remains <see cref="OrderStatus.Pending"/>.
    /// </summary>
    public void MarkPaymentInitiated(string gatewayReference, PaymentMethod method)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Payment can only be initiated while the order is pending.");

        if (PaymentStatus is not PaymentStatus.Pending and not PaymentStatus.Failed)
            throw new DomainException("Payment has already been initiated or completed for this order.");

        if (string.IsNullOrWhiteSpace(gatewayReference))
            throw new DomainException("Gateway reference is required.");

        if (PaymentStatus == PaymentStatus.Failed)
            FailedGatewayPaymentReference = null;

        PaymentGatewayReference = gatewayReference.Trim();
        InitiatedPaymentMethod = method;
        PaymentStatus = PaymentStatus.Initiated;
        UpdatedAt = DateTimeOffset.UtcNow;
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

    /// <summary>
    /// Records a definitive payment failure from the provider while the order remains <see cref="OrderStatus.Pending"/>.
    /// </summary>
    public void MarkPaymentFailed(string providerPaymentReference, string? reason)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Payment failure can only be recorded while the order is pending.");

        if (PaymentStatus == PaymentStatus.Paid)
            throw new DomainException("Cannot record payment failure after payment is confirmed.");

        if (string.IsNullOrWhiteSpace(providerPaymentReference))
            throw new DomainException("Provider payment reference is required.");

        string trimmedRef = providerPaymentReference.Trim();

        if (PaymentStatus == PaymentStatus.Failed
            && string.Equals(FailedGatewayPaymentReference, trimmedRef, StringComparison.OrdinalIgnoreCase))
            return;

        FailedGatewayPaymentReference = trimmedRef;
        PaymentStatus = PaymentStatus.Failed;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new PaymentFailedEvent(Id, trimmedRef, string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()));
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
        AddDomainEvent(new OrderCancelledEvent(Id, CustomerId, CancellationReason));
    }

    public void UpdateStatus(OrderStatus next)
    {
        if (!Status.CanTransitionTo(next))
            throw new DomainException($"Cannot transition order status from {Status} to {next}.");

        Status = next;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Forces <see cref="Status"/> to <paramref name="newStatus"/> without <see cref="OrderStatusExtensions.CanTransitionTo"/> checks (platform administrator intervention).
    /// </summary>
    public void OverrideStatusByAdmin(OrderStatus newStatus, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Override reason is required.");

        string trimmed = reason.Trim();
        if (trimmed.Length > MaxAdminOverrideReasonLength)
            throw new DomainException($"Override reason cannot exceed {MaxAdminOverrideReasonLength} characters.");

        if (Status == newStatus)
            return;

        OrderStatus previous = Status;
        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new OrderStatusAdminOverriddenEvent(Id, previous, newStatus, trimmed));
    }

    /// <summary>
    /// Records that goods for this order arrived at the warehouse while <see cref="OrderStatus.Paid"/>; moves to <see cref="OrderStatus.AtWarehouse"/> and raises <see cref="ItemArrivedAtWarehouseEvent"/>.
    /// </summary>
    /// <param name="representativeWarehouseItemId">One of the created <c>WarehouseItem</c> ids (used in the domain event payload).</param>
    public void MarkArrivedAtWarehouse(Guid representativeWarehouseItemId)
    {
        if (Status != OrderStatus.Paid)
            throw new DomainException($"Order must be paid before arrival can be recorded. Current status: {Status}.");

        if (representativeWarehouseItemId == Guid.Empty)
            throw new DomainException("Warehouse item id is required.");

        UpdateStatus(OrderStatus.AtWarehouse);
        AddDomainEvent(new ItemArrivedAtWarehouseEvent(Id, representativeWarehouseItemId));
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
