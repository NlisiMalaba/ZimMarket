using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Admin;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ReadModels;
using ZimMarket.Shared;

namespace ZimMarket.Application.Tests.Admin;

public sealed class GetPendingKycQueryHandlerTests
{
    [Fact]
    public async Task Non_admin_returns_forbidden()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var fileStorage = Substitute.For<IFileStorage>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Seller);

        var handler = new GetPendingKycQueryHandler(
            unitOfWork,
            currentUser,
            fileStorage,
            NullLogger<GetPendingKycQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetPendingKycQuery(UserRole.Seller, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AdminKycErrorCodes.Forbidden);
        await unitOfWork.PendingKyc.DidNotReceive()
            .GetPagedPendingReviewAsync(Arg.Any<UserRole>(), Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Admin_seller_queue_maps_sas_urls()
    {
        Guid userId = Guid.NewGuid();
        var row = new PendingKycQueueRow(
            userId,
            "seller@example.com",
            "Seller One",
            UserRole.Seller,
            "Biz",
            null,
            null,
            "kyc-documents/user/nid.pdf",
            "kyc-documents/user/por.pdf",
            null,
            null);

        var page = new PagedList<PendingKycQueueRow>([row], 1, 20, 1);

        var pending = Substitute.For<IPendingKycReadRepository>();
        pending.GetPagedPendingReviewAsync(UserRole.Seller, Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>())
            .Returns(page);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.PendingKyc.Returns(pending);

        var fileStorage = Substitute.For<IFileStorage>();
        fileStorage
            .GenerateSasUrlAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult($"https://blob/{ci.ArgAt<string>(0)}"));

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);

        var handler = new GetPendingKycQueryHandler(
            unitOfWork,
            currentUser,
            fileStorage,
            NullLogger<GetPendingKycQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetPendingKycQuery(UserRole.Seller, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        PendingKycQueueItemDto item = result.Value.Items[0];
        item.UserId.Should().Be(userId);
        item.NationalId.Should().NotBeNull();
        item.NationalId!.Url.Should().Contain("kyc-documents/user/nid.pdf");
        item.ProofOfResidence.Should().NotBeNull();
        item.LicenseDocument.Should().BeNull();
        item.VehicleDocument.Should().BeNull();
    }
}
