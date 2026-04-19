using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;
using ZimMarket.Domain.ValueObjects;

namespace ZimMarket.Application.Admin;

public sealed class CreateAdminCommandHandler : IRequestHandler<CreateAdminCommand, Result<Guid>>
{
    private const int SyntheticPhoneAttempts = 32;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IUserIdentityReadRepository _userIdentityRead;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly ILogger<CreateAdminCommandHandler> _logger;

    public CreateAdminCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IUserIdentityReadRepository userIdentityRead,
        IPasswordHasher<User> passwordHasher,
        IEmailService emailService,
        ILogger<CreateAdminCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _userIdentityRead = userIdentityRead ?? throw new ArgumentNullException(nameof(userIdentityRead));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Guid>> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserId == Guid.Empty
            || _currentUser.Role != UserRole.SuperAdmin)
        {
            _logger.LogDebug("Create admin rejected: caller is not a super administrator.");
            return Result<Guid>.Failure(
                CreateAdminErrorCodes.Forbidden,
                "Only super administrators can create administrator accounts.");
        }

        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        string fullName = request.FullName.Trim();

        if (await _userIdentityRead.ExistsWithEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug("Create admin rejected: email {Email} already exists.", normalizedEmail);
            return Result<Guid>.Failure(AuthErrorCodes.UserAlreadyExists, "This email is already registered.");
        }

        PhoneNumber? phone = await TryAllocateSyntheticPhoneAsync(cancellationToken).ConfigureAwait(false);
        if (phone is null)
        {
            return Result<Guid>.Failure(
                CreateAdminErrorCodes.PhoneAllocationFailed,
                "Could not allocate a unique phone number for the administrator profile.");
        }
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid id = Guid.NewGuid();

        var hashSource = new AdminUser(
            id,
            normalizedEmail,
            fullName,
            phone,
            passwordHash: "TEMP",
            KycStatus.Approved,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            createdAt: now,
            updatedAt: now);

        string passwordHash = _passwordHasher.HashPassword(hashSource, request.Password);

        var admin = new AdminUser(
            id,
            normalizedEmail,
            fullName,
            phone,
            passwordHash,
            KycStatus.Approved,
            isActive: true,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            createdAt: now,
            updatedAt: now);

        await _unitOfWork.RunInTransactionAsync(
                async () =>
                {
                    await _unitOfWork.Admins.AddAsync(admin, cancellationToken).ConfigureAwait(false);
                    return admin.Id;
                },
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await _emailService
                .SendAsync(BuildCredentialsMessage(fullName, normalizedEmail, request.Password), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Administrator {UserId} ({Email}) was created but the credentials email could not be sent.",
                id,
                normalizedEmail);
        }

        _logger.LogInformation(
            "Administrator {UserId} ({Email}) created by super admin {SuperAdminId}.",
            id,
            normalizedEmail,
            _currentUser.UserId);

        return Result<Guid>.Success(id);
    }

    private async Task<PhoneNumber?> TryAllocateSyntheticPhoneAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < SyntheticPhoneAttempts; i++)
        {
            string candidate = $"+263{Random.Shared.Next(100000000, 1000000000)}";
            ZimMarket.Shared.Result<PhoneNumber> created = PhoneNumber.Create(candidate);
            if (created.IsFailure)
                continue;

            if (await _userIdentityRead.ExistsWithPhoneAsync(created.Value!, cancellationToken).ConfigureAwait(false))
                continue;

            return created.Value;
        }

        _logger.LogError("Exhausted synthetic phone attempts when creating an administrator.");
        return null;
    }

    private static EmailMessage BuildCredentialsMessage(string fullName, string email, string password) =>
        new()
        {
            To = email,
            Subject = "Your ZimMarket administrator account",
            IsHtml = false,
            Body =
                $"""
                Hello {fullName},

                A ZimMarket super administrator has created an administrator account for you.

                Sign-in email: {email}
                Temporary password: {password}

                Please sign in as soon as possible and change your password from the account security settings.

                If you were not expecting this message, contact your platform owner immediately.

                — ZimMarket
                """
        };
}
