using FluentAssertions;
using ZimMarket.Domain.Entities.Warehouse;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Exceptions;

namespace ZimMarket.Domain.Tests.Warehouse;

public sealed class WarehouseItemQcTests
{
    [Fact]
    public void ApplyQcOutcome_pending_to_failed_then_passed()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var item = WarehouseItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), now, now, now).Value!;

        item.ApplyQcOutcome(WarehouseQcStatus.Failed, true, "See supervisor");
        item.QcStatus.Should().Be(WarehouseQcStatus.Failed);
        item.QcNotes.Should().Be("See supervisor");

        item.ApplyQcOutcome(WarehouseQcStatus.Passed, replaceNotes: false, notes: null);
        item.QcStatus.Should().Be(WarehouseQcStatus.Passed);
        item.QcNotes.Should().Be("See supervisor");
    }

    [Fact]
    public void ApplyQcOutcome_after_passed_throws()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var item = WarehouseItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), now, now, now).Value!;
        item.ApplyQcOutcome(WarehouseQcStatus.Passed, false, null);

        var act = () => item.ApplyQcOutcome(WarehouseQcStatus.Failed, true, "x");
        act.Should().Throw<DomainException>();
    }
}
