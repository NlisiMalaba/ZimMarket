using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Sellers;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Sellers;

public sealed class SellerProfileHandlerTests
{
    private static readonly PhoneNumber TestPhone = PhoneNumber.Create("+263771234567").Value!;

    [Fact]
    public async Task ChangeSellerPassword_wrong_current_password_returns_failure()
    {
        var seller = CreateSeller();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Role.Returns(UserRole.Seller);
        currentUser.UserId.Returns(seller.Id);

        var userLogin = Substitute.For<IUserLoginRepository>();
        userLogin.GetTrackedByIdAsync(seller.Id, Arg.Any<CancellationToken>()).Returns(seller);

        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        passwordHasher
            .VerifyHashedPassword(seller, seller.PasswordHash, "WrongPassword1")
            .Returns(PasswordVerificationResult.Failed);

        var handler = new ChangeSellerPasswordCommandHandler(
            currentUser,
            userLogin,
            Substitute.For<IUnitOfWork>(),
            passwordHasher);

        var result = await handler.Handle(
            new ChangeSellerPasswordCommand("WrongPassword1", "NewPassword1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Seller.InvalidPassword");
    }

    private static Seller CreateSeller() =>
        new(
            Guid.NewGuid(),
            "seller@example.com",
            "Seller Name",
            TestPhone,
            "HASH",
            KycStatus.Approved,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            "Test Shop",
            "kyc/national",
            "kyc/proof",
            isApproved: true,
            rejectionReason: null);
}
