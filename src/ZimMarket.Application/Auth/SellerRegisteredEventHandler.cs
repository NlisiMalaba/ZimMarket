using MediatR;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Domain.Events;
using ZimMarket.Domain.Interfaces;

namespace ZimMarket.Application.Auth;

/// <summary>
/// Sends the seller welcome email after persistence (raised from <see cref="SellerRegisteredEvent"/>).
/// </summary>
public sealed class SellerRegisteredEventHandler : INotificationHandler<SellerRegisteredEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<SellerRegisteredEventHandler> _logger;

    public SellerRegisteredEventHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<SellerRegisteredEventHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(SellerRegisteredEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var seller = await _unitOfWork.Sellers
                .GetByIdAsync(notification.SellerId, cancellationToken)
                .ConfigureAwait(false);

            if (seller is null)
            {
                _logger.LogWarning(
                    "SellerRegisteredEvent for unknown seller {SellerId}; welcome email skipped.",
                    notification.SellerId);

                return;
            }

            string body =
                $"""
                Hello {seller.FullName},

                Welcome to ZimMarket — your seller account for "{seller.BusinessName}" is ready.

                Next steps — seller verification (KYC):
                1. Sign in to the seller portal using the account you just created.
                2. Upload your national ID document and proof of residence when prompted.
                3. Submit your documents for review. We will notify you when verification is complete.

                If you did not register as a seller, please ignore this email or contact support.

                — ZimMarket
                """;

            await _emailService
                .SendAsync(
                    new EmailMessage
                    {
                        To = seller.Email,
                        Subject = "Welcome to ZimMarket — complete your seller verification",
                        Body = body,
                        IsHtml = false
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send welcome email for seller {SellerId}. Registration remains committed.",
                notification.SellerId);
        }
    }
}
