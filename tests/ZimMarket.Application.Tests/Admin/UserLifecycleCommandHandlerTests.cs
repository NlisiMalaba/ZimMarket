using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Admin;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Admin;

public sealed class UserLifecycleCommandHandlerTests
{
    private static Customer CreateTrackedCustomer(Guid id, bool active = true)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PhoneNumber phone = PhoneNumber.Create("+263712345678").Value!;
        var customer = new Customer(
            id,
            "c@example.com",
            "Customer",
            phone,
            passwordHash: "HASH",
            KycStatus.Approved,
            isActive: active,
            refreshTokenHash: "rt",
            refreshTokenExpiry: now.AddDays(1),
            createdAt: now,
            updatedAt: now,
            pushNotificationToken: null);

        return customer;
    }

    [Fact]
    public async Task Deactivate_non_admin_forbidden()
    {
        var userLogin = Substitute.For<IUserLoginRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Customer);

        var handler = new DeactivateUserCommandHandler(
            currentUser,
            userLogin,
            NullLogger<DeactivateUserCommandHandler>.Instance);

        var result = await handler.Handle(new DeactivateUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserLifecycleErrorCodes.Forbidden);
        await userLogin.DidNotReceive().GetTrackedByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivate_admin_cannot_touch_super_admin()
    {
        Guid targetId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PhoneNumber phone = PhoneNumber.Create("+263798765432").Value!;
        var superAdmin = new SuperAdminUser(
            targetId,
            "s@example.com",
            "Super",
            phone,
            "HASH",
            KycStatus.Approved,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            now,
            now);

        var userLogin = Substitute.For<IUserLoginRepository>();
        userLogin.GetTrackedByIdAsync(targetId, Arg.Any<CancellationToken>()).Returns(superAdmin);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);

        var handler = new DeactivateUserCommandHandler(
            currentUser,
            userLogin,
            NullLogger<DeactivateUserCommandHandler>.Instance);

        var result = await handler.Handle(new DeactivateUserCommand(targetId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserLifecycleErrorCodes.InsufficientPrivilegeForTarget);
        superAdmin.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_admin_clears_refresh_and_sets_inactive_for_customer()
    {
        Guid targetId = Guid.NewGuid();
        Customer customer = CreateTrackedCustomer(targetId, active: true);

        var userLogin = Substitute.For<IUserLoginRepository>();
        userLogin.GetTrackedByIdAsync(targetId, Arg.Any<CancellationToken>()).Returns(customer);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);

        var handler = new DeactivateUserCommandHandler(
            currentUser,
            userLogin,
            NullLogger<DeactivateUserCommandHandler>.Instance);

        var result = await handler.Handle(new DeactivateUserCommand(targetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        customer.IsActive.Should().BeFalse();
        customer.RefreshTokenHash.Should().BeNull();
        customer.RefreshTokenExpiry.Should().BeNull();
    }

    [Fact]
    public async Task Activate_super_admin_can_reactivate_admin()
    {
        Guid targetId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PhoneNumber phone = PhoneNumber.Create("+263711111111").Value!;
        var admin = new AdminUser(
            targetId,
            "a@example.com",
            "Admin",
            phone,
            "HASH",
            KycStatus.Approved,
            isActive: false,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            now,
            now);

        var userLogin = Substitute.For<IUserLoginRepository>();
        userLogin.GetTrackedByIdAsync(targetId, Arg.Any<CancellationToken>()).Returns(admin);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.SuperAdmin);

        var handler = new ActivateUserCommandHandler(
            currentUser,
            userLogin,
            NullLogger<ActivateUserCommandHandler>.Instance);

        var result = await handler.Handle(new ActivateUserCommand(targetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        admin.IsActive.Should().BeTrue();
    }
}
