using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Common.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentInitiateResult> InitiateAsync(
        PaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentWebhookResult> VerifyWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default);
}
