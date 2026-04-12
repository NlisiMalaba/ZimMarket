using ZimMarket.Domain.Enums;
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
        DateTimeOffset updatedAt)
    {
        if (orderId == Guid.Empty)
            return Result<WarehouseItem>.Failure("Order id is required.");

        if (productId == Guid.Empty)
            return Result<WarehouseItem>.Failure("Product id is required.");

        var item = new WarehouseItem
        {
            Id = id,
            OrderId = orderId,
            ProductId = productId,
            ArrivedAt = arrivedAt,
            QcStatus = WarehouseQcStatus.Pending,
            QcNotes = null,
            PackagedAt = null,
            BatchId = null,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        return Result<WarehouseItem>.Success(item);
    }
}
