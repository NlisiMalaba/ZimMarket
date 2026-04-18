using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZimMarket.API.Http;
using ZimMarket.Application.Auth;
using ZimMarket.Application.Payments;
using ZimMarket.Domain.Enums;

namespace ZimMarket.API.Controllers.V1;

[ApiController]
[Route("api/v1/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(ISender sender, ILogger<PaymentsController> logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("initiate")]
    [Authorize(Policy = AuthorizationPolicies.Customer)]
    public async Task<IActionResult> InitiatePayment(
        [FromBody] InitiatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        string idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        var command = new InitiatePaymentCommand(
            request.OrderId,
            request.PaymentMethod,
            idempotencyKey);

        return (await _sender.Send(command, cancellationToken).ConfigureAwait(false))
            .ToOkActionResult(HttpContext);
    }

    [HttpPost("webhook/paynow")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessPaynowWebhook(CancellationToken cancellationToken)
    {
        await ProcessWebhookInternalAsync(PaymentGatewayType.Paynow, cancellationToken).ConfigureAwait(false);
        return Ok(new ApiSuccessResponse<object?>(null));
    }

    [HttpPost("webhook/ecocash")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessEcocashWebhook(CancellationToken cancellationToken)
    {
        await ProcessWebhookInternalAsync(PaymentGatewayType.Ecocash, cancellationToken).ConfigureAwait(false);
        return Ok(new ApiSuccessResponse<object?>(null));
    }

    private async Task ProcessWebhookInternalAsync(
        PaymentGatewayType gatewayType,
        CancellationToken cancellationToken)
    {
        string payload = await ReadRequestBodyAsync(cancellationToken).ConfigureAwait(false);
        string? signature = ReadWebhookSignature(gatewayType);

        var command = new ProcessPaymentWebhookCommand(payload, signature, gatewayType);
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Payment webhook processing failed for {Gateway}. Code={ErrorCode}, Message={Message}",
                gatewayType,
                result.ErrorCode,
                result.ErrorMessage);
        }
    }

    private async Task<string> ReadRequestBodyAsync(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        Request.Body.Position = 0;

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        string payload = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        Request.Body.Position = 0;
        return payload;
    }

    private string? ReadWebhookSignature(PaymentGatewayType gatewayType)
    {
        if (gatewayType == PaymentGatewayType.Paynow)
        {
            return FirstNonEmptyHeader("X-Paynow-Signature", "X-Signature", "Hash");
        }

        return FirstNonEmptyHeader("X-Ecocash-Signature", "X-Signature", "Hash");
    }

    private string? FirstNonEmptyHeader(params string[] names)
    {
        foreach (string name in names)
        {
            string value = Request.Headers[name].ToString().Trim();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    public sealed record InitiatePaymentRequest(Guid OrderId, PaymentMethod PaymentMethod);
}
