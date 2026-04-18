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
/// Notifies the seller of KYC approval (SMS + email). JWT <c>kycStatus</c> is re-issued on login or refresh from persisted <see cref="User.KycStatus"/>.
/// </summary>
public sealed class SellerApprovedEventHandler : INotificationHandler<SellerApprovedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<SellerApprovedEventHandler> _logger;

    public SellerApprovedEventHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<SellerApprovedEventHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(SellerApprovedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            Seller? seller = await _unitOfWork.Sellers
                .GetByIdAsync(notification.SellerId, cancellationToken)
                .ConfigureAwait(false);

            if (seller is null)
            {
                _logger.LogWarning(
                    "SellerApprovedEvent for unknown seller {SellerId}; notifications skipped.",
                    notification.SellerId);

                return;
            }

            _logger.LogInformation(
                "Seller {SellerId} KYC approved; JWT kycStatus claim will show {KycStatus} after login or token refresh.",
                seller.Id,
                KycStatus.Approved);

            string emailBody =
                $"""
                Hello {seller.FullName},

                Your seller verification (KYC) for "{seller.BusinessName}" has been approved on ZimMarket.

                Your access token includes a kycStatus claim. After this approval, sign in again or call the refresh endpoint so your client receives an updated token.

                — ZimMarket
                """;

            await _emailService
                .SendAsync(
                    new EmailMessage
                    {
                        To = seller.Email,
                        Subject = "ZimMarket: Your seller verification is approved",
                        Body = emailBody,
                        IsHtml = false
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            string sms =
                $"ZimMarket: Your seller account ({seller.BusinessName}) KYC is approved. Check your email for details.";

            await _smsService
                .SendAsync(seller.PhoneNumber.Value, sms, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send seller KYC approval notifications for {SellerId}. Approval remains committed.",
                notification.SellerId);
        }
    }
}
