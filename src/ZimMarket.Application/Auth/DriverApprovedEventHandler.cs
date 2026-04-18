using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Enums;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Auth;

/// <summary>
/// Notifies the driver of KYC approval (SMS + email). JWT <c>kycStatus</c> is re-issued on login or refresh from persisted <see cref="User.KycStatus"/>.
/// </summary>
public sealed class DriverApprovedEventHandler : INotificationHandler<DriverApprovedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<DriverApprovedEventHandler> _logger;

    public DriverApprovedEventHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<DriverApprovedEventHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(DriverApprovedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            Driver? driver = await _unitOfWork.Drivers
                .GetByIdAsync(notification.DriverId, cancellationToken)
                .ConfigureAwait(false);

            if (driver is null)
            {
                _logger.LogWarning(
                    "DriverApprovedEvent for unknown driver {DriverId}; notifications skipped.",
                    notification.DriverId);

                return;
            }

            _logger.LogInformation(
                "Driver {DriverId} KYC approved; JWT kycStatus claim will show {KycStatus} after login or token refresh.",
                driver.Id,
                KycStatus.Approved);

            string emailBody =
                $"""
                Hello {driver.FullName},

                Your driver verification (KYC) has been approved on ZimMarket.

                Your access token includes a kycStatus claim. After this approval, sign in again or call the refresh endpoint so your client receives an updated token.

                — ZimMarket
                """;

            await _emailService
                .SendAsync(
                    new EmailMessage
                    {
                        To = driver.Email,
                        Subject = "ZimMarket: Your driver verification is approved",
                        Body = emailBody,
                        IsHtml = false
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            const string sms = "ZimMarket: Your driver account KYC is approved. Check your email for details.";

            await _smsService
                .SendAsync(driver.PhoneNumber.Value, sms, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send driver KYC approval notifications for {DriverId}. Approval remains committed.",
                notification.DriverId);
        }
    }
}
