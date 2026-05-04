using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Infrastructure.Configuration;

namespace ZimMarket.Infrastructure.Notifications;

public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.To))
            throw new ArgumentException("Email recipient is required.", nameof(message));

        if (string.IsNullOrWhiteSpace(message.Subject))
            throw new ArgumentException("Email subject is required.", nameof(message));

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = message.Subject.Trim(),
            Body = message.Body ?? string.Empty,
            IsBodyHtml = message.IsHtml
        };

        mailMessage.To.Add(message.To.Trim());

        if (!string.IsNullOrWhiteSpace(message.ReplyTo))
            mailMessage.ReplyToList.Add(new MailAddress(message.ReplyTo.Trim()));

        if (message.Cc is not null)
        {
            foreach (string cc in message.Cc.Where(static x => !string.IsNullOrWhiteSpace(x)))
                mailMessage.CC.Add(cc.Trim());
        }

        if (message.Bcc is not null)
        {
            foreach (string bcc in message.Bcc.Where(static x => !string.IsNullOrWhiteSpace(x)))
                mailMessage.Bcc.Add(bcc.Trim());
        }

        using var smtpClient = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.User, _options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        cancellationToken.ThrowIfCancellationRequested();
        await smtpClient.SendMailAsync(mailMessage).ConfigureAwait(false);
        _logger.LogInformation("Email sent via SMTP to {Recipient}.", message.To);
    }
}
