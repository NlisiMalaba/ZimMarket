using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Entities.Users;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Auth;

/// <summary>
/// Sends the seller a KYC rejection email and SMS including the admin reason.
/// </summary>
public sealed class SellerRejectedEventHandler : INotificationHandler<SellerRejectedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<SellerRejectedEventHandler> _logger;

    public SellerRejectedEventHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<SellerRejectedEventHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(SellerRejectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            Seller? seller = await _unitOfWork.Sellers
                .GetByIdAsync(notification.SellerId, cancellationToken)
                .ConfigureAwait(false);

            if (seller is null)
            {
                _logger.LogWarning(
                    "SellerRejectedEvent for unknown seller {SellerId}; email skipped.",
                    notification.SellerId);

                return;
            }

            string reason = string.IsNullOrWhiteSpace(notification.Reason)
                ? "No additional details were provided."
                : notification.Reason.Trim();

            string emailBody =
                $"""
                Hello {seller.FullName},

                We were not able to approve your seller verification (KYC) for "{seller.BusinessName}" at this time.

                Reason:
                {reason}

                You may sign in to ZimMarket and submit corrected documents when you are ready.

                — ZimMarket
                """;

            await _emailService
                .SendAsync(
                    new EmailMessage
                    {
                        To = seller.Email,
                        Subject = "ZimMarket: Seller verification update",
                        Body = emailBody,
                        IsHtml = false
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            string sms =
                $"ZimMarket: Your seller verification ({seller.BusinessName}) was not approved. Check your email for details.";

            await _smsService
                .SendAsync(seller.PhoneNumber.Value, sms, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send seller KYC rejection notifications for {SellerId}. Rejection remains committed.",
                notification.SellerId);
        }
    }
}
