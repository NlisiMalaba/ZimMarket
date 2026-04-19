using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Admin;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Admin;

public sealed class ApproveKycCommandHandlerTests
{
    private static readonly PhoneNumber TestPhone = PhoneNumber.Create("+263771234567").Value!;

    [Fact]
    public async Task Non_admin_returns_forbidden()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Seller);

        var handler = new ApproveKycCommandHandler(
            unitOfWork,
            currentUser,
            NullLogger<ApproveKycCommandHandler>.Instance);

        var result = await handler.Handle(
            new ApproveKycCommand(Guid.NewGuid(), UserRole.Seller),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AdminKycErrorCodes.Forbidden);
        await unitOfWork.Sellers.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Admin_approves_pending_seller_calls_update()
    {
        Guid sellerId = Guid.NewGuid();
        var seller = new Seller(
            sellerId,
            "s@example.com",
            "Name",
            TestPhone,
            "hash",
            KycStatus.PendingReview,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow,
            businessName: "Biz",
            nationalIdDocumentKey: "kyc-documents/x/id.pdf",
            proofOfResidenceDocumentKey: "kyc-documents/x/por.pdf",
            isApproved: false,
            rejectionReason: null);

        var sellers = Substitute.For<IUserRepository<Seller>>();
        sellers.GetByIdAsync(sellerId, Arg.Any<CancellationToken>()).Returns(seller);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Sellers.Returns(sellers);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);

        var handler = new ApproveKycCommandHandler(
            unitOfWork,
            currentUser,
            NullLogger<ApproveKycCommandHandler>.Instance);

        var result = await handler.Handle(new ApproveKycCommand(sellerId, UserRole.Seller), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        seller.KycStatus.Should().Be(KycStatus.Approved);
        seller.IsApproved.Should().BeTrue();
        await sellers.Received(1).UpdateAsync(seller, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Admin_approve_non_pending_seller_returns_cannot_approve()
    {
        Guid sellerId = Guid.NewGuid();
        var seller = new Seller(
            sellerId,
            "s@example.com",
            "Name",
            TestPhone,
            "hash",
            KycStatus.NotSubmitted,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow,
            businessName: "Biz",
            nationalIdDocumentKey: string.Empty,
            proofOfResidenceDocumentKey: string.Empty,
            isApproved: false,
            rejectionReason: null);

        var sellers = Substitute.For<IUserRepository<Seller>>();
        sellers.GetByIdAsync(sellerId, Arg.Any<CancellationToken>()).Returns(seller);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Sellers.Returns(sellers);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);

        var handler = new ApproveKycCommandHandler(
            unitOfWork,
            currentUser,
            NullLogger<ApproveKycCommandHandler>.Instance);

        var result = await handler.Handle(new ApproveKycCommand(sellerId, UserRole.Seller), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AdminKycErrorCodes.CannotApprove);
        await sellers.DidNotReceive().UpdateAsync(Arg.Any<Seller>(), Arg.Any<CancellationToken>());
    }
}
