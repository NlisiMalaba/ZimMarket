using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Logistics;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Shared;

namespace ZimMarket.Application.Tests.Logistics;

public sealed class GetBatchesQueryHandlerTests
{
    [Fact]
    public async Task Non_admin_returns_LOGISTICS_FORBIDDEN()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new GetBatchesQueryHandler(
            unitOfWork,
            CreateCustomerCurrentUser(),
            NullLogger<GetBatchesQueryHandler>.Instance);

        var result = await handler.Handle(new GetBatchesQuery(null, 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(LogisticsErrorCodes.LogisticsForbidden);
        await unitOfWork.DeliveryBatches.DidNotReceive()
            .GetPagedAsync(Arg.Any<DeliveryBatchStatus?>(), Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Admin_returns_paged_list()
    {
        Guid batchId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid warehouseId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow.AddDays(-1);
        var updated = DateTimeOffset.UtcNow;
        DeliveryBatch batch = DeliveryBatch.Create(
            batchId,
            driverId,
            warehouseId,
            [Guid.NewGuid(), Guid.NewGuid()],
            created,
            updated).Value!;

        var paged = new PagedList<DeliveryBatch>(new[] { batch }, 1, 20, 1);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var batches = Substitute.For<IDeliveryBatchRepository>();
        unitOfWork.DeliveryBatches.Returns(batches);
        batches.GetPagedAsync(null, Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>())
            .Returns(paged);

        var handler = new GetBatchesQueryHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            NullLogger<GetBatchesQueryHandler>.Instance);

        var result = await handler.Handle(new GetBatchesQuery(null, 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        var dto = result.Value.Items.Single();
        dto.BatchId.Should().Be(batchId);
        dto.DriverId.Should().Be(driverId);
        dto.WarehouseId.Should().Be(warehouseId);
        dto.Status.Should().Be(DeliveryBatchStatus.Created);
        dto.OrderCount.Should().Be(2);
        dto.CreatedAtUtc.Should().Be(created);
        dto.UpdatedAtUtc.Should().Be(updated);
    }

    private static ICurrentUser CreateCustomerCurrentUser()
    {
        var u = Substitute.For<ICurrentUser>();
        u.IsAuthenticated.Returns(true);
        u.UserId.Returns(Guid.NewGuid());
        u.Role.Returns(UserRole.Customer);
        return u;
    }

    private static ICurrentUser CreateAdminCurrentUser()
    {
        var u = Substitute.For<ICurrentUser>();
        u.IsAuthenticated.Returns(true);
        u.UserId.Returns(Guid.NewGuid());
        u.Role.Returns(UserRole.Admin);
        return u;
    }
}
