using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Application.Auth;

public sealed class VerifyAdminEmailCommandHandler : IRequestHandler<VerifyAdminEmailCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthTokenService _authTokenService;
    private readonly IUserLoginRepository _userLoginRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<VerifyAdminEmailCommandHandler> _logger;

    public VerifyAdminEmailCommandHandler(
        IUnitOfWork unitOfWork,
        IAuthTokenService authTokenService,
        IUserLoginRepository userLoginRepository,
        IEmailService emailService,
        ILogger<VerifyAdminEmailCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _authTokenService = authTokenService ?? throw new ArgumentNullException(nameof(authTokenService));
        _userLoginRepository = userLoginRepository ?? throw new ArgumentNullException(nameof(userLoginRepository));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(VerifyAdminEmailCommand request, CancellationToken cancellationToken)
    {
        string tokenHash = _authTokenService.HashToken(request.Token);
        AuthToken? token = await _unitOfWork.AuthTokens
            .GetActiveByHashAsync(tokenHash, AuthTokenPurpose.AdminEmailVerification, cancellationToken)
            .ConfigureAwait(false);

        if (token is null)
            return Result.Failure(AuthErrorCodes.AuthEmailVerificationInvalid, "Email verification token is invalid or expired.");

        var admin = await _userLoginRepository.GetTrackedByIdAsync(token.UserId, cancellationToken).ConfigureAwait(false);
        if (admin is null || admin.Role != UserRole.Admin)
            return Result.Failure(AuthErrorCodes.AuthEmailVerificationInvalid, "Email verification token is invalid or expired.");

        AdminApprovalState? state = await _unitOfWork.AdminApprovalStates
            .GetByUserIdAsync(admin.Id, cancellationToken)
            .ConfigureAwait(false);

        if (state is null)
            return Result.Failure(AuthErrorCodes.AuthEmailVerificationInvalid, "Admin approval state was not found.");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        token.MarkConsumed(now);
        state.MarkEmailVerified(now);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        IReadOnlyList<string> superAdminEmails = await _unitOfWork.AdminApprovalStates
            .GetSuperAdminEmailsAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (string superAdminEmail in superAdminEmails)
        {
            await _emailService.SendAsync(
                    new EmailMessage
                    {
                        To = superAdminEmail,
                        Subject = "Admin approval required",
                        IsHtml = false,
                        Body =
                            $"""
                            Administrator registration requires your approval.

                            Admin email: {admin.Email}
                            Admin name: {admin.FullName}
                            Admin user id: {admin.Id}

                            Approve this admin from the super admin panel.
                            """
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        _logger.LogInformation("Admin {AdminUserId} verified email and is pending super admin approval.", admin.Id);
        return Result.Success();
    }
}
