using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Infrastructure.Configuration;

namespace ZimMarket.Infrastructure.Notifications;

public sealed class SendGridEmailService : IEmailService
{
    private readonly SendGridClient _client;
    private readonly SendGridOptions _options;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IOptions<SendGridOptions> options, ILogger<SendGridEmailService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value;
        _client = new SendGridClient(_options.ApiKey);
    }

    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.To))
            throw new ArgumentException("Email recipient is required.", nameof(message));

        if (string.IsNullOrWhiteSpace(message.Subject))
            throw new ArgumentException("Email subject is required.", nameof(message));

        var from = new EmailAddress(_options.FromEmail, _options.FromName);
        var to = new EmailAddress(message.To.Trim());

        SendGridMessage sgMessage;
        if (message.IsHtml)
        {
            const string plainFallback =
                "This email is formatted in HTML. Please open it in an HTML-capable mail client.";
            sgMessage = MailHelper.CreateSingleEmail(
                from,
                to,
                message.Subject.Trim(),
                plainFallback,
                message.Body ?? string.Empty);
        }
        else
        {
            sgMessage = MailHelper.CreateSingleEmail(
                from,
                to,
                message.Subject.Trim(),
                message.Body ?? string.Empty,
                htmlContent: null);
        }

        if (!string.IsNullOrWhiteSpace(message.ReplyTo))
            sgMessage.SetReplyTo(new EmailAddress(message.ReplyTo.Trim()));

        if (message.Cc is not null)
        {
            foreach (string cc in message.Cc)
            {
                if (!string.IsNullOrWhiteSpace(cc))
                    sgMessage.AddCc(cc.Trim());
            }
        }

        if (message.Bcc is not null)
        {
            foreach (string bcc in message.Bcc)
            {
                if (!string.IsNullOrWhiteSpace(bcc))
                    sgMessage.AddBcc(bcc.Trim());
            }
        }

        Response response = await _client.SendEmailAsync(sgMessage, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            string responseBody = await response.Body.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(
                "SendGrid rejected email. Status {StatusCode}. Response {ResponseBody}.",
                response.StatusCode,
                responseBody);
            throw new InvalidOperationException($"SendGrid rejected the email ({(int)response.StatusCode}).");
        }

        _logger.LogInformation("Email sent via SendGrid. Status {StatusCode}.", response.StatusCode);
    }
}
