using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Application.Auth;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserLoginRepository _userLoginRepository;
    private readonly IAuthTokenService _authTokenService;
    private readonly IAuthLinkBuilder _authLinkBuilder;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IUserLoginRepository userLoginRepository,
        IAuthTokenService authTokenService,
        IAuthLinkBuilder authLinkBuilder,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userLoginRepository = userLoginRepository ?? throw new ArgumentNullException(nameof(userLoginRepository));
        _authTokenService = authTokenService ?? throw new ArgumentNullException(nameof(authTokenService));
        _authLinkBuilder = authLinkBuilder ?? throw new ArgumentNullException(nameof(authLinkBuilder));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        User? user = await _userLoginRepository.GetTrackedByNormalizedEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return Result.Success();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string rawToken = _authTokenService.GenerateRawToken();

        await _unitOfWork.AuthTokens
            .RevokeActiveTokensAsync(user.Id, AuthTokenPurpose.PasswordReset, now, cancellationToken)
            .ConfigureAwait(false);

        await _unitOfWork.AuthTokens.AddAsync(
                new AuthToken(
                    Guid.NewGuid(),
                    user.Id,
                    AuthTokenPurpose.PasswordReset,
                    _authTokenService.HashToken(rawToken),
                    _authTokenService.GetExpiry(AuthTokenPurpose.PasswordReset, now),
                    now,
                    now),
                cancellationToken)
            .ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        string resetLink = _authLinkBuilder.BuildResetPasswordLink(rawToken);
        await _emailService.SendAsync(
                new EmailMessage
                {
                    To = user.Email,
                    Subject = "Reset your ZimMarket password",
                    IsHtml = false,
                    Body =
                        $"""
                        You requested a password reset.
                        Use this link to set a new password:
                        {resetLink}

                        This link expires in 30 minutes.
                        """
                },
                cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Password reset requested for user {UserId}.", user.Id);
        return Result.Success();
    }
}
