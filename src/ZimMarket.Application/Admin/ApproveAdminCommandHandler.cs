using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Interfaces;
using ZimMarket.Domain.Interfaces.Repositories;

namespace ZimMarket.Application.Admin;

public sealed class ApproveAdminCommandHandler : IRequestHandler<ApproveAdminCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IUserLoginRepository _userLoginRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<ApproveAdminCommandHandler> _logger;

    public ApproveAdminCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IUserLoginRepository userLoginRepository,
        IEmailService emailService,
        ILogger<ApproveAdminCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _userLoginRepository = userLoginRepository ?? throw new ArgumentNullException(nameof(userLoginRepository));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(ApproveAdminCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Role != UserRole.SuperAdmin || _currentUser.UserId == Guid.Empty)
            return Result.Failure(CreateAdminErrorCodes.Forbidden, "Only super administrators can approve admins.");

        var admin = await _userLoginRepository.GetTrackedByIdAsync(request.AdminUserId, cancellationToken).ConfigureAwait(false);
        if (admin is null || admin.Role != UserRole.Admin)
            return Result.Failure("ADMIN_NOT_FOUND", "Administrator account was not found.");

        AdminApprovalState? state = await _unitOfWork.AdminApprovalStates
            .GetByUserIdAsync(request.AdminUserId, cancellationToken)
            .ConfigureAwait(false);

        if (state is null || !state.IsEmailVerified)
            return Result.Failure(AuthErrorCodes.AuthEmailVerificationRequired, "Administrator email is not yet verified.");

        if (state.IsApproved)
            return Result.Success();

        admin.Activate();
        state.MarkApproved(_currentUser.UserId, DateTimeOffset.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _emailService.SendAsync(
                new EmailMessage
                {
                    To = admin.Email,
                    Subject = "Your ZimMarket admin account was approved",
                    IsHtml = false,
                    Body =
                        """
                        Your administrator account has been approved by a super admin.
                        You can now sign in to the ZimMarket admin portal.
                        """
                },
                cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Admin {AdminUserId} approved by super admin {SuperAdminUserId}.", request.AdminUserId, _currentUser.UserId);
        return Result.Success();
    }
}
