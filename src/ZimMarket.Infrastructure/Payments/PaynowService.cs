using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Infrastructure.Configuration;

namespace ZimMarket.Infrastructure.Payments;

/// <summary>
/// Paynow Zimbabwe <see cref="IPaymentGateway"/> using SHA512 request/response hashing per Paynow documentation.
/// </summary>
public sealed class PaynowService : IPaymentGateway
{
    private const string StatusField = "status";
    private const string HashField = "hash";
    private const string BrowserUrlField = "browserurl";
    private const string PollUrlField = "pollurl";
    private const string ReferenceField = "reference";
    private const string PaynowReferenceField = "paynowreference";
    private const string ErrorField = "error";

    private readonly HttpClient _httpClient;
    private readonly PaynowOptions _options;
    private readonly ILogger<PaynowService> _logger;
    private readonly IHostEnvironment _hostEnvironment;

    public PaynowService(
        HttpClient httpClient,
        IOptions<PaynowOptions> options,
        ILogger<PaynowService> logger,
        IHostEnvironment hostEnvironment)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
    }

    /// <inheritdoc />
    public async Task<PaymentInitiateResult> InitiateAsync(
        PaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (CanLogPaymentDetailsAtDebug())
        {
            _logger.LogDebug(
                "Paynow InitiateAsync for order {OrderId}, amount {Amount}, currency {Currency}.",
                request.OrderId,
                request.Amount,
                request.Currency);
        }
        else
        {
            _logger.LogDebug("Paynow InitiateAsync invoked.");
        }

        string reference = request.OrderId.ToString("N");
        string amount = request.Amount.ToString("F2", CultureInfo.InvariantCulture);
        string returnUrl = string.Format(CultureInfo.InvariantCulture, _options.ReturnUrlTemplate, request.OrderId);
        string resultUrl = string.Format(CultureInfo.InvariantCulture, _options.ResultUrlTemplate, request.OrderId);

        var fields = new List<KeyValuePair<string, string>>
        {
            new("id", _options.IntegrationId.ToString(CultureInfo.InvariantCulture)),
            new("reference", reference),
            new("amount", amount)
        };

        if (!string.IsNullOrWhiteSpace(request.Description))
            fields.Add(new KeyValuePair<string, string>("additionalinfo", request.Description.Trim()));

        fields.Add(new KeyValuePair<string, string>("returnurl", returnUrl));
        fields.Add(new KeyValuePair<string, string>("resulturl", resultUrl));

        TryAddMetadataField(fields, "authemail", request.Metadata);
        TryAddMetadataField(fields, "authphone", request.Metadata);
        TryAddMetadataField(fields, "authname", request.Metadata);
        TryAddMetadataField(fields, "merchanttrace", request.Metadata);

        fields.Add(new KeyValuePair<string, string>(StatusField, "Message"));

        string hash = PaynowProtocol.ComputeOutboundHash(fields, _options.IntegrationKey);
        fields.Add(new KeyValuePair<string, string>(HashField, hash));

        string body = BuildUrlEncodedBody(fields);
        using var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")
        {
            CharSet = Encoding.UTF8.WebName
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient
                .PostAsync(_options.InitiateTransactionUrl, content, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Paynow initiate HTTP request failed.");
            return new PaymentInitiateResult
            {
                Success = false,
                ErrorCode = "paynow_http_error",
                ErrorMessage = "Unable to reach Paynow."
            };
        }

        string responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        List<KeyValuePair<string, string>> responsePairs = PaynowProtocol.ParseForm(responseText);
        string? status = GetFirstValueIgnoreCase(responsePairs, StatusField);

        if (!string.Equals(status, "Ok", StringComparison.OrdinalIgnoreCase))
        {
            string? error = GetFirstValueIgnoreCase(responsePairs, ErrorField);
            _logger.LogWarning(
                "Paynow initiate returned non-Ok status {PaynowStatus}: {PaynowError}.",
                status ?? "(null)",
                error ?? "(none)");

            return new PaymentInitiateResult
            {
                Success = false,
                ErrorCode = "paynow_initiate_rejected",
                ErrorMessage = error ?? "Paynow rejected the transaction request."
            };
        }

        string? receivedHash = GetFirstValueIgnoreCase(responsePairs, HashField);
        string computedHash = PaynowProtocol.ComputeInboundHash(responsePairs, _options.IntegrationKey);
        if (!PaynowProtocol.ConstantTimeEqualsHex(computedHash, receivedHash))
        {
            _logger.LogWarning("Paynow initiate response hash verification failed.");
            return new PaymentInitiateResult
            {
                Success = false,
                ErrorCode = "paynow_invalid_hash",
                ErrorMessage = "Paynow response could not be verified."
            };
        }

        string? browserUrl = GetFirstValueIgnoreCase(responsePairs, BrowserUrlField);
        string? pollUrl = GetFirstValueIgnoreCase(responsePairs, PollUrlField);

        if (string.IsNullOrWhiteSpace(browserUrl))
        {
            return new PaymentInitiateResult
            {
                Success = false,
                ErrorCode = "paynow_missing_browserurl",
                ErrorMessage = "Paynow response did not include a checkout URL."
            };
        }

        _logger.LogInformation("Paynow transaction initiated successfully for checkout redirect.");

        return new PaymentInitiateResult
        {
            Success = true,
            RedirectUrl = browserUrl,
            PollUrl = pollUrl,
            ExternalPaymentId = pollUrl
        };
    }

    /// <inheritdoc />
    public Task<PaymentWebhookResult> VerifyWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return Task.FromResult(new PaymentWebhookResult
            {
                IsValid = false,
                ErrorMessage = "Empty webhook payload."
            });
        }

        List<KeyValuePair<string, string>> pairs = PaynowProtocol.ParseForm(payload);
        string? receivedHash = GetFirstValueIgnoreCase(pairs, HashField);
        if (!string.IsNullOrWhiteSpace(signature))
            receivedHash ??= signature.Trim();

        if (string.IsNullOrEmpty(receivedHash))
        {
            return Task.FromResult(new PaymentWebhookResult
            {
                IsValid = false,
                ErrorMessage = "Missing hash."
            });
        }

        string computed = PaynowProtocol.ComputeInboundHash(pairs, _options.IntegrationKey);
        if (!PaynowProtocol.ConstantTimeEqualsHex(computed, receivedHash))
        {
            _logger.LogWarning("Paynow webhook hash verification failed.");
            return Task.FromResult(new PaymentWebhookResult
            {
                IsValid = false,
                ErrorMessage = "Invalid signature."
            });
        }

        string? reference = GetFirstValueIgnoreCase(pairs, ReferenceField);
        Guid? orderId = TryParseOrderReference(reference);
        string? paynowRef = GetFirstValueIgnoreCase(pairs, PaynowReferenceField);
        string? status = GetFirstValueIgnoreCase(pairs, StatusField);

        if (CanLogPaymentDetailsAtDebug())
        {
            _logger.LogDebug(
                "Paynow webhook verified. Order {OrderId}, status {Status}.",
                orderId,
                status);
        }
        else
        {
            _logger.LogDebug("Paynow webhook verified.");
        }

        return Task.FromResult(new PaymentWebhookResult
        {
            IsValid = true,
            OrderId = orderId,
            PaymentReference = paynowRef,
            Status = status
        });
    }

    /// <inheritdoc />
    public async Task<PaymentPollResult> PollStatusAsync(string pollUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pollUrl))
        {
            return new PaymentPollResult
            {
                Success = false,
                HashValid = false,
                ErrorMessage = "Poll URL is required."
            };
        }

        if (!Uri.TryCreate(pollUrl.Trim(), UriKind.Absolute, out Uri? pollUri) || !IsTrustedPaynowHost(pollUri))
        {
            _logger.LogWarning("Rejected Paynow poll URL with untrusted host.");
            return new PaymentPollResult
            {
                Success = false,
                HashValid = false,
                ErrorMessage = "Invalid poll URL."
            };
        }

        if (CanLogPaymentDetailsAtDebug())
            _logger.LogDebug("Polling Paynow status at configured poll URL.");
        else
            _logger.LogDebug("Polling Paynow status.");

        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/x-www-form-urlencoded");
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(pollUri, content, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Paynow poll HTTP request failed.");
            return new PaymentPollResult
            {
                Success = false,
                HashValid = false,
                ErrorMessage = "Unable to reach Paynow for polling."
            };
        }

        string responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        List<KeyValuePair<string, string>> pairs = PaynowProtocol.ParseForm(responseText);
        string? receivedHash = GetFirstValueIgnoreCase(pairs, HashField);
        if (string.IsNullOrEmpty(receivedHash))
        {
            return new PaymentPollResult
            {
                Success = false,
                HashValid = false,
                ErrorMessage = "Poll response missing hash."
            };
        }

        string computed = PaynowProtocol.ComputeInboundHash(pairs, _options.IntegrationKey);
        if (!PaynowProtocol.ConstantTimeEqualsHex(computed, receivedHash))
        {
            _logger.LogWarning("Paynow poll response hash verification failed.");
            return new PaymentPollResult
            {
                Success = false,
                HashValid = false,
                ErrorMessage = "Poll response could not be verified."
            };
        }

        string? reference = GetFirstValueIgnoreCase(pairs, ReferenceField);
        Guid? orderId = TryParseOrderReference(reference);
        string? paynowRef = GetFirstValueIgnoreCase(pairs, PaynowReferenceField);
        string? status = GetFirstValueIgnoreCase(pairs, StatusField);

        if (string.Equals(status, "NotFound", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "Error", StringComparison.OrdinalIgnoreCase))
        {
            string? err = GetFirstValueIgnoreCase(pairs, ErrorField);
            return new PaymentPollResult
            {
                Success = false,
                HashValid = true,
                Status = status,
                OrderId = orderId,
                PaymentReference = paynowRef,
                ErrorMessage = err ?? status
            };
        }

        return new PaymentPollResult
        {
            Success = true,
            HashValid = true,
            Status = status,
            OrderId = orderId,
            PaymentReference = paynowRef
        };
    }

    private bool CanLogPaymentDetailsAtDebug() => !_hostEnvironment.IsProduction();

    private static void TryAddMetadataField(
        List<KeyValuePair<string, string>> fields,
        string key,
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || !metadata.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
            return;

        fields.Add(new KeyValuePair<string, string>(key, value.Trim()));
    }

    private static string BuildUrlEncodedBody(IReadOnlyList<KeyValuePair<string, string>> fields)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < fields.Count; i++)
        {
            if (i > 0)
                sb.Append('&');

            KeyValuePair<string, string> pair = fields[i];
            sb.Append(Uri.EscapeDataString(pair.Key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(pair.Value));
        }

        return sb.ToString();
    }

    private static string? GetFirstValueIgnoreCase(
        IReadOnlyList<KeyValuePair<string, string>> pairs,
        string key)
    {
        foreach (KeyValuePair<string, string> pair in pairs)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        }

        return null;
    }

    private static Guid? TryParseOrderReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        if (Guid.TryParseExact(reference.Trim(), "N", out Guid g))
            return g;

        return Guid.TryParse(reference.Trim(), out g) ? g : null;
    }

    private static bool IsTrustedPaynowHost(Uri uri)
    {
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        string host = uri.Host;
        return string.Equals(host, "www.paynow.co.zw", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "staging.paynow.co.zw", StringComparison.OrdinalIgnoreCase);
    }
}
