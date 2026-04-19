using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ZimMarket.Application.Admin;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Tests.Admin;

public sealed class CreateAdminCommandHandlerTests
{
    [Fact]
    public async Task Non_super_admin_returns_forbidden()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Admin);

        var handler = new CreateAdminCommandHandler(
            unitOfWork,
            currentUser,
            Substitute.For<IUserIdentityReadRepository>(),
            Substitute.For<IPasswordHasher<User>>(),
            Substitute.For<IEmailService>(),
            NullLogger<CreateAdminCommandHandler>.Instance);

        Result<Guid> result = await handler.Handle(
            new CreateAdminCommand("a@b.com", "Password1", "Full"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(CreateAdminErrorCodes.Forbidden);
        await unitOfWork.DidNotReceive().RunInTransactionAsync(Arg.Any<Func<Task<Guid>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Super_admin_persists_and_sends_credentials_email()
    {
        string email = "newadmin@example.com";

        var identityRead = Substitute.For<IUserIdentityReadRepository>();
        identityRead.ExistsWithEmailAsync(email, Arg.Any<CancellationToken>()).Returns(false);
        identityRead.ExistsWithPhoneAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>()).Returns(false);

        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        passwordHasher.HashPassword(Arg.Any<User>(), "Password1").Returns("HASHED");

        var emailService = Substitute.For<IEmailService>();

        var admins = Substitute.For<IUserRepository<AdminUser>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Admins.Returns(admins);
        unitOfWork.RunInTransactionAsync(Arg.Any<Func<Task<Guid>>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task<Guid>>>()());

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.SuperAdmin);

        var handler = new CreateAdminCommandHandler(
            unitOfWork,
            currentUser,
            identityRead,
            passwordHasher,
            emailService,
            NullLogger<CreateAdminCommandHandler>.Instance);

        Result<Guid> result = await handler.Handle(
            new CreateAdminCommand(email, "Password1", "New Admin"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await admins.Received(1).AddAsync(Arg.Any<AdminUser>(), Arg.Any<CancellationToken>());
        await emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == email && m.Body.Contains("Password1")),
            Arg.Any<CancellationToken>());
    }
}
