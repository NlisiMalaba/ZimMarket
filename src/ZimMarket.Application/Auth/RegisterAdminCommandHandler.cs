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

namespace ZimMarket.Application.Auth;

public sealed class RegisterAdminCommandHandler : IRequestHandler<RegisterAdminCommand, Result>
{
    private const int SyntheticPhoneAttempts = 32;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserIdentityReadRepository _userIdentityRead;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IAuthTokenService _authTokenService;
    private readonly IAuthLinkBuilder _authLinkBuilder;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterAdminCommandHandler> _logger;

    public RegisterAdminCommandHandler(
        IUnitOfWork unitOfWork,
        IUserIdentityReadRepository userIdentityRead,
        IPasswordHasher<User> passwordHasher,
        IAuthTokenService authTokenService,
        IAuthLinkBuilder authLinkBuilder,
        IEmailService emailService,
        ILogger<RegisterAdminCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userIdentityRead = userIdentityRead ?? throw new ArgumentNullException(nameof(userIdentityRead));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _authTokenService = authTokenService ?? throw new ArgumentNullException(nameof(authTokenService));
        _authLinkBuilder = authLinkBuilder ?? throw new ArgumentNullException(nameof(authLinkBuilder));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(RegisterAdminCommand request, CancellationToken cancellationToken)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        string fullName = request.FullName.Trim();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (await _userIdentityRead.ExistsWithEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(AuthErrorCodes.UserAlreadyExists, "This email is already registered.");
        }

        PhoneNumber? phone = await TryAllocateSyntheticPhoneAsync(cancellationToken).ConfigureAwait(false);
        if (phone is null)
        {
            return Result.Failure("ADMIN_PHONE_ALLOCATION_FAILED", "Could not allocate phone for the administrator account.");
        }

        bool hasSuperAdmin = await _unitOfWork.AdminApprovalStates
            .ExistsAnySuperAdminAsync(cancellationToken)
            .ConfigureAwait(false);

        Guid userId = Guid.NewGuid();
        var hashSource = new AdminUser(
            userId,
            normalizedEmail,
            fullName,
            phone,
            "TEMP",
            KycStatus.Approved,
            isActive: !hasSuperAdmin,
            refreshTokenHash: null,
            refreshTokenExpiry: null,
            now,
            now);

        string passwordHash = _passwordHasher.HashPassword(hashSource, request.Password);
        User createdUser = hasSuperAdmin
            ? new AdminUser(
                userId,
                normalizedEmail,
                fullName,
                phone,
                passwordHash,
                KycStatus.Approved,
                isActive: false,
                refreshTokenHash: null,
                refreshTokenExpiry: null,
                now,
                now)
            : new SuperAdminUser(
                userId,
                normalizedEmail,
                fullName,
                phone,
                passwordHash,
                KycStatus.Approved,
                isActive: true,
                refreshTokenHash: null,
                refreshTokenExpiry: null,
                now,
                now);

        string? verificationTokenRaw = null;
        if (hasSuperAdmin)
            verificationTokenRaw = _authTokenService.GenerateRawToken();

        await _unitOfWork.RunInTransactionAsync(
                async () =>
                {
                    if (createdUser is AdminUser adminUser)
                    {
                        await _unitOfWork.Admins.AddAsync(adminUser, cancellationToken).ConfigureAwait(false);
                        await _unitOfWork.AdminApprovalStates
                            .AddAsync(new AdminApprovalState(adminUser.Id, now, now), cancellationToken)
                            .ConfigureAwait(false);

                        await _unitOfWork.AuthTokens.RevokeActiveTokensAsync(
                                adminUser.Id,
                                AuthTokenPurpose.AdminEmailVerification,
                                now,
                                cancellationToken)
                            .ConfigureAwait(false);

                        await _unitOfWork.AuthTokens.AddAsync(
                                new AuthToken(
                                    Guid.NewGuid(),
                                    adminUser.Id,
                                    AuthTokenPurpose.AdminEmailVerification,
                                    _authTokenService.HashToken(verificationTokenRaw!),
                                    _authTokenService.GetExpiry(AuthTokenPurpose.AdminEmailVerification, now),
                                    now,
                                    now),
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await _unitOfWork.SuperAdmins.AddAsync((SuperAdminUser)createdUser, cancellationToken).ConfigureAwait(false);
                    }

                    return 0;
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (!hasSuperAdmin)
        {
            _logger.LogInformation("Bootstrapped first platform user as super admin {Email}.", normalizedEmail);
            return Result.Success();
        }

        string verificationUrl = _authLinkBuilder.BuildAdminEmailVerificationLink(verificationTokenRaw!);
        await _emailService.SendAsync(
                BuildAdminVerificationMessage(normalizedEmail, fullName, verificationUrl),
                cancellationToken)
            .ConfigureAwait(false);

        return Result.Success();
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

        return null;
    }

    private static EmailMessage BuildAdminVerificationMessage(string email, string fullName, string verificationUrl) =>
        new()
        {
            To = email,
            Subject = "Verify your ZimMarket admin email",
            IsHtml = false,
            Body =
                $"""
                Hello {fullName},

                You registered as a ZimMarket administrator.
                Verify your email using this link:
                {verificationUrl}

                After verifying, your account will remain pending until a super admin approves it.
                """
        };
}
