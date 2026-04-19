using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Auth;

/// <summary>
/// Sends the driver a KYC rejection email and SMS including the admin reason.
/// </summary>
public sealed class DriverRejectedEventHandler : INotificationHandler<DriverRejectedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<DriverRejectedEventHandler> _logger;

    public DriverRejectedEventHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<DriverRejectedEventHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(DriverRejectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            Driver? driver = await _unitOfWork.Drivers
                .GetByIdAsync(notification.DriverId, cancellationToken)
                .ConfigureAwait(false);

            if (driver is null)
            {
                _logger.LogWarning(
                    "DriverRejectedEvent for unknown driver {DriverId}; email skipped.",
                    notification.DriverId);

                return;
            }

            string reason = string.IsNullOrWhiteSpace(notification.Reason)
                ? "No additional details were provided."
                : notification.Reason.Trim();

            string emailBody =
                $"""
                Hello {driver.FullName},

                We were not able to approve your driver verification (KYC) at this time.

                Reason:
                {reason}

                You may sign in to ZimMarket and submit corrected documents when you are ready.

                — ZimMarket
                """;

            await _emailService
                .SendAsync(
                    new EmailMessage
                    {
                        To = driver.Email,
                        Subject = "ZimMarket: Driver verification update",
                        Body = emailBody,
                        IsHtml = false
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            const string sms = "ZimMarket: Your driver verification was not approved. Check your email for details.";

            await _smsService
                .SendAsync(driver.PhoneNumber.Value, sms, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send driver KYC rejection notifications for {DriverId}. Rejection remains committed.",
                notification.DriverId);
        }
    }
}
