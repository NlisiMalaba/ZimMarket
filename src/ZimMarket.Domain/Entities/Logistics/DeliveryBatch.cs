using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Shared;

namespace ZimMarket.Domain.Entities.Logistics;

public sealed class DeliveryBatch : BaseEntity
{
    private readonly List<Guid> _orderIds = [];

    private DeliveryBatch()
    {
    }

    public Guid DriverId { get; private set; }

    public IReadOnlyList<Guid> OrderIds => _orderIds;

    public DeliveryBatchStatus Status { get; private set; }

    public Guid WarehouseId { get; private set; }

    public DateTimeOffset? CollectedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static Result<DeliveryBatch> Create(
        Guid id,
        Guid driverId,
        Guid warehouseId,
        IReadOnlyList<Guid> orderIds,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (driverId == Guid.Empty)
            return Result<DeliveryBatch>.Failure("Driver is required.");

        if (warehouseId == Guid.Empty)
            return Result<DeliveryBatch>.Failure("Warehouse is required.");

        if (orderIds.Count == 0)
            return Result<DeliveryBatch>.Failure("Batch must include at least one order.");

        if (orderIds.Any(o => o == Guid.Empty))
            return Result<DeliveryBatch>.Failure("Order id cannot be empty.");

        if (orderIds.Count != orderIds.Distinct().Count())
            return Result<DeliveryBatch>.Failure("Order ids must be unique.");

        var batch = new DeliveryBatch
        {
            Id = id,
            DriverId = driverId,
            WarehouseId = warehouseId,
            Status = DeliveryBatchStatus.Created,
            CollectedAt = null,
            CompletedAt = null,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        batch._orderIds.AddRange(orderIds);
        batch.AddDomainEvent(new BatchCreatedEvent(id, driverId));

        return Result<DeliveryBatch>.Success(batch);
    }

    public void AssignDriver(Guid driverId)
    {
        if (Status == DeliveryBatchStatus.Completed)
            throw new DomainException("Cannot reassign driver on a completed batch.");

        if (driverId == Guid.Empty)
            throw new DomainException("Driver is required.");

        DriverId = driverId;
        Touch();
        AddDomainEvent(new BatchDriverAssignedEvent(Id, driverId));
    }

    public void MarkCollected()
    {
        if (Status != DeliveryBatchStatus.Created)
            throw new DomainException("Batch can only be marked collected from the created state.");

        Status = DeliveryBatchStatus.Collected;
        CollectedAt = DateTimeOffset.UtcNow;
        Touch();
        AddDomainEvent(new BatchCollectedEvent(Id));
    }

    public void MarkInTransit()
    {
        if (Status != DeliveryBatchStatus.Collected)
            throw new DomainException("Batch can only move in transit after collection.");

        Status = DeliveryBatchStatus.InTransit;
        Touch();
        AddDomainEvent(new BatchInTransitEvent(Id));
    }

    public void Complete()
    {
        if (Status != DeliveryBatchStatus.InTransit)
            throw new DomainException("Batch can only be completed while in transit.");

        Status = DeliveryBatchStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        Touch();
        AddDomainEvent(new DeliveryCompletedEvent(Id, DriverId));
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
