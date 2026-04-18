using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Auth;

public sealed class AuthHandlerUnitTests
{
    private static readonly PhoneNumber TestPhone = PhoneNumber.Create("+263771234567").Value!;

    private static readonly PhoneNumber AltPhone = PhoneNumber.Create("+263772345678").Value!;

    [Fact]
    public async Task RegisterCustomer_duplicate_email_returns_USER_ALREADY_EXISTS()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var userIdentityRead = Substitute.For<IUserIdentityReadRepository>();
        userIdentityRead.ExistsWithEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new RegisterCustomerCommandHandler(
            unitOfWork,
            userIdentityRead,
            Substitute.For<IPasswordHasher<User>>(),
            Substitute.For<IJwtService>(),
            NullLogger<RegisterCustomerCommandHandler>.Instance);

        var result = await handler.Handle(
            new RegisterCustomerCommand("new@example.com", TestPhone.Value, "Password1", "Full Name", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AuthErrorCodes.UserAlreadyExists);
    }

    [Fact]
    public async Task RegisterCustomer_duplicate_phone_returns_USER_PHONE_ALREADY_EXISTS()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var userIdentityRead = Substitute.For<IUserIdentityReadRepository>();
        userIdentityRead.ExistsWithEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        userIdentityRead.ExistsWithPhoneAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new RegisterCustomerCommandHandler(
            unitOfWork,
            userIdentityRead,
            Substitute.For<IPasswordHasher<User>>(),
            Substitute.For<IJwtService>(),
            NullLogger<RegisterCustomerCommandHandler>.Instance);

        var result = await handler.Handle(
            new RegisterCustomerCommand("new@example.com", TestPhone.Value, "Password1", "Full Name", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AuthErrorCodes.UserPhoneAlreadyExists);
    }

    [Fact]
    public async Task RegisterCustomer_success_returns_tokens()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var customers = Substitute.For<IUserRepository<Customer>>();
        unitOfWork.Customers.Returns(customers);

        var userIdentityRead = Substitute.For<IUserIdentityReadRepository>();
        userIdentityRead.ExistsWithEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        userIdentityRead.ExistsWithPhoneAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        passwordHasher.HashPassword(Arg.Any<User>(), Arg.Any<string>()).Returns("hashed");

        var jwt = Substitute.For<IJwtService>();
        jwt.GenerateRefreshToken().Returns("refresh-raw");
        jwt.HashRefreshTokenForStorage(Arg.Any<string>()).Returns("refresh-hash");
        jwt.GetRefreshTokenExpiresAtUtc().Returns(DateTimeOffset.UtcNow.AddDays(30));
        jwt.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<UserRole>(), Arg.Any<KycStatus>())
            .Returns("access-token");

        var handler = new RegisterCustomerCommandHandler(
            unitOfWork,
            userIdentityRead,
            passwordHasher,
            jwt,
            NullLogger<RegisterCustomerCommandHandler>.Instance);

        var result = await handler.Handle(
            new RegisterCustomerCommand("ok@example.com", TestPhone.Value, "Password1", "Full Name", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-raw");
        result.Value.KycStatus.Should().Be(KycStatus.NotSubmitted);

        await customers.Received(1).AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        jwt.Received(1).GenerateAccessToken(
            Arg.Any<Guid>(),
            "ok@example.com",
            UserRole.Customer,
            KycStatus.NotSubmitted);
    }

    [Fact]
    public async Task Login_wrong_password_returns_AUTH_INVALID_CREDENTIALS()
    {
        var userLogin = Substitute.For<IUserLoginRepository>();
        var user = CreateActiveCustomer();
        userLogin.GetTrackedByNormalizedEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        passwordHasher.VerifyHashedPassword(user, user.PasswordHash, Arg.Any<string>())
            .Returns(PasswordVerificationResult.Failed);

        var handler = new LoginQueryHandler(
            userLogin,
            Substitute.For<IUnitOfWork>(),
            passwordHasher,
            Substitute.For<IJwtService>(),
            NullLogger<LoginQueryHandler>.Instance);

        var result = await handler.Handle(
            new LoginQuery(user.Email, "WrongPassword1", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AuthErrorCodes.AuthInvalidCredentials);
    }

    [Fact]
    public async Task Login_deactivated_account_returns_AUTH_ACCOUNT_LOCKED()
    {
        var userLogin = Substitute.For<IUserLoginRepository>();
        var user = CreateActiveCustomer();
        user.Deactivate();

        userLogin.GetTrackedByNormalizedEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        passwordHasher.VerifyHashedPassword(user, user.PasswordHash, Arg.Any<string>())
            .Returns(PasswordVerificationResult.Success);

        var handler = new LoginQueryHandler(
            userLogin,
            Substitute.For<IUnitOfWork>(),
            passwordHasher,
            Substitute.For<IJwtService>(),
            NullLogger<LoginQueryHandler>.Instance);

        var result = await handler.Handle(
            new LoginQuery(user.Email, "Password1", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AuthErrorCodes.AuthAccountLocked);
    }

    [Fact]
    public async Task RefreshToken_expired_refresh_session_returns_AUTH_REFRESH_INVALID()
    {
        var userId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", userId.ToString("D"))]));

        var jwt = Substitute.For<IJwtService>();
        jwt.TryValidateAccessTokenForRefresh(Arg.Any<string>())
            .Returns(new AccessTokenForRefreshPrincipal(principal, DateTimeOffset.UtcNow.AddMinutes(-5)));

        var user = CreateActiveCustomer(userId);
        user.SetRefreshToken("stored-hash", DateTimeOffset.UtcNow.AddHours(-1));

        var userLogin = Substitute.For<IUserLoginRepository>();
        userLogin.GetTrackedByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new RefreshTokenCommandHandler(
            userLogin,
            jwt,
            NullLogger<RefreshTokenCommandHandler>.Instance);

        var result = await handler.Handle(
            new RefreshTokenCommand("any-access", "refresh-raw"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AuthErrorCodes.AuthRefreshInvalid);
    }

    [Fact]
    public async Task RefreshToken_valid_rotation_returns_new_pair_and_invalidates_old_refresh()
    {
        Guid userId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", userId.ToString("D"))]));

        var jwt = Substitute.For<IJwtService>();
        jwt.TryValidateAccessTokenForRefresh(Arg.Any<string>())
            .Returns(new AccessTokenForRefreshPrincipal(principal, DateTimeOffset.UtcNow.AddMinutes(-5)));
        jwt.GenerateRefreshToken().Returns("new-refresh");
        jwt.HashRefreshTokenForStorage(Arg.Any<string>())
            .Returns(ci => "HASH:" + ci.ArgAt<string>(0));
        jwt.VerifyRefreshToken(Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => ci.ArgAt<string>(1) == "HASH:" + ci.ArgAt<string>(0));
        jwt.GetRefreshTokenExpiresAtUtc().Returns(DateTimeOffset.UtcNow.AddDays(30));
        jwt.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<UserRole>(), Arg.Any<KycStatus>())
            .Returns("new-access");

        var user = CreateActiveCustomer(userId);
        const string oldRefresh = "old-refresh";
        user.SetRefreshToken("HASH:" + oldRefresh, DateTimeOffset.UtcNow.AddDays(1));

        var userLogin = Substitute.For<IUserLoginRepository>();
        userLogin.GetTrackedByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new RefreshTokenCommandHandler(
            userLogin,
            jwt,
            NullLogger<RefreshTokenCommandHandler>.Instance);

        var result = await handler.Handle(
            new RefreshTokenCommand("expired-access", oldRefresh),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("new-access");
        result.Value.RefreshToken.Should().Be("new-refresh");

        jwt.VerifyRefreshToken(oldRefresh, user.RefreshTokenHash!).Should().BeFalse();
    }

    [Fact]
    public async Task SubmitSellerKyc_non_seller_returns_Kyc_Forbidden()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Role.Returns(UserRole.Customer);

        var handler = new SubmitSellerKycCommandHandler(
            currentUser,
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IFileStorage>(),
            NullLogger<SubmitSellerKycCommandHandler>.Instance);

        var result = await handler.Handle(
            new SubmitSellerKycCommand("id-key", "proof-key"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Kyc.Forbidden");
    }

    [Fact]
    public async Task SubmitSellerKyc_already_submitted_returns_Kyc_AlreadySubmitted()
    {
        Guid sellerId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Role.Returns(UserRole.Seller);
        currentUser.UserId.Returns(sellerId);

        var phone = PhoneNumber.Create("+263773456789").Value!;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var seller = new Seller(
            sellerId,
            "s@example.com",
            "Seller",
            phone,
            "hash",
            KycStatus.PendingReview,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            createdAt: now,
            updatedAt: now,
            businessName: "Biz",
            nationalIdDocumentKey: "nid",
            proofOfResidenceDocumentKey: "por",
            isApproved: false,
            rejectionReason: null);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sellers = Substitute.For<IUserRepository<Seller>>();
        unitOfWork.Sellers.Returns(sellers);
        sellers.GetByIdAsync(sellerId, Arg.Any<CancellationToken>()).Returns(seller);

        var fileStorage = Substitute.For<IFileStorage>();
        fileStorage.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = new SubmitSellerKycCommandHandler(
            currentUser,
            unitOfWork,
            fileStorage,
            NullLogger<SubmitSellerKycCommandHandler>.Instance);

        var result = await handler.Handle(
            new SubmitSellerKycCommand("new-nid", "new-por"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Kyc.AlreadySubmitted");
    }

    private static Customer CreateActiveCustomer(Guid? id = null)
    {
        Guid userId = id ?? Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new Customer(
            userId,
            "user@example.com",
            "User Name",
            AltPhone,
            passwordHash: "stored-hash",
            KycStatus.NotSubmitted,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            createdAt: now,
            updatedAt: now,
            pushNotificationToken: null);
    }
}
