using FluentAssertions;
using ZimMarket.Domain.Entities.Warehouse;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;

namespace ZimMarket.Domain.Tests;

public sealed class WarehouseItemAssignDeliveryBatchTests
{
    [Fact]
    public void AssignToDeliveryBatch_sets_batch_id_when_qc_passed()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        WarehouseItem item = WarehouseItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now,
            now,
            now).Value!;

        item.ApplyQcOutcome(WarehouseQcStatus.Passed, false, null);
        Guid batchId = Guid.NewGuid();

        item.AssignToDeliveryBatch(batchId);

        item.BatchId.Should().Be(batchId);
    }

    [Fact]
    public void AssignToDeliveryBatch_throws_when_not_passed_qc()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        WarehouseItem item = WarehouseItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now,
            now,
            now).Value!;

        Action act = () => item.AssignToDeliveryBatch(Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }
}
