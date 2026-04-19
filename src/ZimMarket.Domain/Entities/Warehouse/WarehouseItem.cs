using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;
using ZimMarket.Shared;

namespace ZimMarket.Domain.Entities.Warehouse;

public sealed class WarehouseItem : BaseEntity
{
    private WarehouseItem()
    {
    }

    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public DateTimeOffset ArrivedAt { get; private set; }

    public WarehouseQcStatus QcStatus { get; private set; }

    public string? QcNotes { get; private set; }

    public DateTimeOffset? PackagedAt { get; private set; }

    public Guid? BatchId { get; private set; }

    public static Result<WarehouseItem> Create(
        Guid id,
        Guid orderId,
        Guid productId,
        DateTimeOffset arrivedAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string? receivingNotes = null)
    {
        if (orderId == Guid.Empty)
            return Result<WarehouseItem>.Failure("Order id is required.");

        if (productId == Guid.Empty)
            return Result<WarehouseItem>.Failure("Product id is required.");

        string? notes = string.IsNullOrWhiteSpace(receivingNotes) ? null : receivingNotes.Trim();

        var item = new WarehouseItem
        {
            Id = id,
            OrderId = orderId,
            ProductId = productId,
            ArrivedAt = arrivedAt,
            QcStatus = WarehouseQcStatus.Pending,
            QcNotes = notes,
            PackagedAt = null,
            BatchId = null,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        return Result<WarehouseItem>.Success(item);
    }

    /// <summary>
    /// Records QC as <see cref="WarehouseQcStatus.Passed"/> or <see cref="WarehouseQcStatus.Failed"/>.
    /// When <paramref name="replaceNotes"/> is true, <see cref="QcNotes"/> is set from <paramref name="notes"/> (trimmed; empty becomes null).
    /// </summary>
    public void ApplyQcOutcome(WarehouseQcStatus outcome, bool replaceNotes, string? notes)
    {
        if (outcome is not (WarehouseQcStatus.Passed or WarehouseQcStatus.Failed))
            throw new DomainException("QC outcome must be Passed or Failed.");

        if (QcStatus == WarehouseQcStatus.Passed)
            throw new DomainException("Cannot change QC after the item has already passed.");

        if (outcome == WarehouseQcStatus.Passed)
        {
            if (QcStatus is not (WarehouseQcStatus.Pending or WarehouseQcStatus.Failed))
                throw new DomainException($"Cannot mark item as passed from QC status {QcStatus}.");
            QcStatus = WarehouseQcStatus.Passed;
        }
        else
        {
            if (QcStatus is not (WarehouseQcStatus.Pending or WarehouseQcStatus.Failed))
                throw new DomainException($"Cannot mark item as failed from QC status {QcStatus}.");
            QcStatus = WarehouseQcStatus.Failed;
        }

        if (replaceNotes)
            QcNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
