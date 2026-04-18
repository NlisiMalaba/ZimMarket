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

    /// <summary>
    /// Poll provider for current payment status (e.g. Paynow empty POST to <c>pollurl</c>).
    /// Gateways that do not support polling return <see cref="PaymentPollResult.Success"/> = false.
    /// </summary>
    Task<PaymentPollResult> PollStatusAsync(
        string pollUrl,
        CancellationToken cancellationToken = default);
}
