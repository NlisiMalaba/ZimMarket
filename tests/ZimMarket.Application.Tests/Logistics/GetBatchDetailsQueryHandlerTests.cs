using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Logistics;
using ZimMarket.Domain.Entities.Logistics;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Application.Tests.Logistics;

public sealed class GetBatchDetailsQueryHandlerTests
{
    [Fact]
    public async Task Unauthenticated_returns_LOGISTICS_FORBIDDEN()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns(Guid.Empty);

        var handler = new GetBatchDetailsQueryHandler(
            unitOfWork,
            currentUser,
            NullLogger<GetBatchDetailsQueryHandler>.Instance);

        var result = await handler.Handle(new GetBatchDetailsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(LogisticsErrorCodes.LogisticsForbidden);
        await unitOfWork.DeliveryBatches.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_batch_returns_BATCH_NOT_FOUND()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var batches = Substitute.For<IDeliveryBatchRepository>();
        unitOfWork.DeliveryBatches.Returns(batches);
        batches.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((DeliveryBatch?)null);

        var handler = new GetBatchDetailsQueryHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            NullLogger<GetBatchDetailsQueryHandler>.Instance);

        var result = await handler.Handle(new GetBatchDetailsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(LogisticsErrorCodes.DeliveryBatchNotFound);
    }

    [Fact]
    public async Task Admin_returns_batch_details()
    {
        Guid batchId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid warehouseId = Guid.NewGuid();
        Guid orderId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow.AddHours(-1);
        var updated = DateTimeOffset.UtcNow;

        DeliveryBatch batch = DeliveryBatch.Create(
            batchId,
            driverId,
            warehouseId,
            [orderId],
            created,
            updated).Value!;

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var batches = Substitute.For<IDeliveryBatchRepository>();
        unitOfWork.DeliveryBatches.Returns(batches);
        batches.GetByIdAsync(batchId, Arg.Any<CancellationToken>()).Returns(batch);

        var handler = new GetBatchDetailsQueryHandler(
            unitOfWork,
            CreateAdminCurrentUser(),
            NullLogger<GetBatchDetailsQueryHandler>.Instance);

        var result = await handler.Handle(new GetBatchDetailsQuery(batchId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BatchId.Should().Be(batchId);
        result.Value.DriverId.Should().Be(driverId);
        result.Value.WarehouseId.Should().Be(warehouseId);
        result.Value.Status.Should().Be(DeliveryBatchStatus.Created);
        result.Value.OrderIds.Should().Equal([orderId]);
        result.Value.CollectedAt.Should().BeNull();
        result.Value.CompletedAt.Should().BeNull();
        result.Value.CreatedAtUtc.Should().Be(created);
        result.Value.UpdatedAtUtc.Should().Be(updated);
    }

    [Fact]
    public async Task SuperAdmin_returns_batch_details()
    {
        Guid batchId = Guid.NewGuid();
        DeliveryBatch batch = DeliveryBatch.Create(
            batchId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            [Guid.NewGuid()],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var batches = Substitute.For<IDeliveryBatchRepository>();
        unitOfWork.DeliveryBatches.Returns(batches);
        batches.GetByIdAsync(batchId, Arg.Any<CancellationToken>()).Returns(batch);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.SuperAdmin);

        var handler = new GetBatchDetailsQueryHandler(
            unitOfWork,
            currentUser,
            NullLogger<GetBatchDetailsQueryHandler>.Instance);

        var result = await handler.Handle(new GetBatchDetailsQuery(batchId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BatchId.Should().Be(batchId);
    }

    [Fact]
    public async Task Driver_assigned_to_batch_returns_details()
    {
        Guid driverId = Guid.NewGuid();
        Guid batchId = Guid.NewGuid();
        DeliveryBatch batch = DeliveryBatch.Create(
            batchId,
            driverId,
            Guid.NewGuid(),
            [Guid.NewGuid()],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var batches = Substitute.For<IDeliveryBatchRepository>();
        unitOfWork.DeliveryBatches.Returns(batches);
        batches.GetByIdAsync(batchId, Arg.Any<CancellationToken>()).Returns(batch);

        var handler = new GetBatchDetailsQueryHandler(
            unitOfWork,
            CreateDriverCurrentUser(driverId),
            NullLogger<GetBatchDetailsQueryHandler>.Instance);

        var result = await handler.Handle(new GetBatchDetailsQuery(batchId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DriverId.Should().Be(driverId);
    }

    [Fact]
    public async Task Driver_not_assigned_returns_BATCH_FORBIDDEN()
    {
        Guid batchId = Guid.NewGuid();
        DeliveryBatch batch = DeliveryBatch.Create(
            batchId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            [Guid.NewGuid()],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var batches = Substitute.For<IDeliveryBatchRepository>();
        unitOfWork.DeliveryBatches.Returns(batches);
        batches.GetByIdAsync(batchId, Arg.Any<CancellationToken>()).Returns(batch);

        var handler = new GetBatchDetailsQueryHandler(
            unitOfWork,
            CreateDriverCurrentUser(Guid.NewGuid()),
            NullLogger<GetBatchDetailsQueryHandler>.Instance);

        var result = await handler.Handle(new GetBatchDetailsQuery(batchId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(LogisticsErrorCodes.DeliveryBatchForbidden);
    }

    [Fact]
    public async Task Customer_returns_BATCH_FORBIDDEN_when_batch_exists()
    {
        Guid batchId = Guid.NewGuid();
        DeliveryBatch batch = DeliveryBatch.Create(
            batchId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            [Guid.NewGuid()],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value!;

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var batches = Substitute.For<IDeliveryBatchRepository>();
        unitOfWork.DeliveryBatches.Returns(batches);
        batches.GetByIdAsync(batchId, Arg.Any<CancellationToken>()).Returns(batch);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Customer);

        var handler = new GetBatchDetailsQueryHandler(
            unitOfWork,
            currentUser,
            NullLogger<GetBatchDetailsQueryHandler>.Instance);

        var result = await handler.Handle(new GetBatchDetailsQuery(batchId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(LogisticsErrorCodes.DeliveryBatchForbidden);
    }

    private static ICurrentUser CreateAdminCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);
        return currentUser;
    }

    private static ICurrentUser CreateDriverCurrentUser(Guid driverId)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(driverId);
        currentUser.Role.Returns(UserRole.Driver);
        return currentUser;
    }
}
