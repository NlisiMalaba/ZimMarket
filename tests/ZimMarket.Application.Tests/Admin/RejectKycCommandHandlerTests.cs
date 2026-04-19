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

public sealed class RejectKycCommandHandlerTests
{
    private static readonly PhoneNumber TestPhone = PhoneNumber.Create("+263771234567").Value!;

    [Fact]
    public async Task Non_admin_returns_forbidden()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Driver);

        var handler = new RejectKycCommandHandler(
            unitOfWork,
            currentUser,
            NullLogger<RejectKycCommandHandler>.Instance);

        var result = await handler.Handle(
            new RejectKycCommand(Guid.NewGuid(), UserRole.Seller, "Missing ID"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AdminKycErrorCodes.Forbidden);
        await unitOfWork.Sellers.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Admin_rejects_pending_seller_calls_update()
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

        var handler = new RejectKycCommandHandler(
            unitOfWork,
            currentUser,
            NullLogger<RejectKycCommandHandler>.Instance);

        var result = await handler.Handle(
            new RejectKycCommand(sellerId, UserRole.Seller, "  Documents unreadable  "),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        seller.KycStatus.Should().Be(KycStatus.Rejected);
        seller.IsApproved.Should().BeFalse();
        seller.RejectionReason.Should().Be("Documents unreadable");
        await sellers.Received(1).UpdateAsync(seller, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Admin_reject_non_pending_seller_returns_cannot_reject()
    {
        Guid sellerId = Guid.NewGuid();
        var seller = new Seller(
            sellerId,
            "s@example.com",
            "Name",
            TestPhone,
            "hash",
            KycStatus.Approved,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow,
            businessName: "Biz",
            nationalIdDocumentKey: "kyc-documents/x/id.pdf",
            proofOfResidenceDocumentKey: "kyc-documents/x/por.pdf",
            isApproved: true,
            rejectionReason: null);

        var sellers = Substitute.For<IUserRepository<Seller>>();
        sellers.GetByIdAsync(sellerId, Arg.Any<CancellationToken>()).Returns(seller);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Sellers.Returns(sellers);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);

        var handler = new RejectKycCommandHandler(
            unitOfWork,
            currentUser,
            NullLogger<RejectKycCommandHandler>.Instance);

        var result = await handler.Handle(
            new RejectKycCommand(sellerId, UserRole.Seller, "Bad docs"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AdminKycErrorCodes.CannotReject);
        await sellers.DidNotReceive().UpdateAsync(Arg.Any<Seller>(), Arg.Any<CancellationToken>());
    }
}
