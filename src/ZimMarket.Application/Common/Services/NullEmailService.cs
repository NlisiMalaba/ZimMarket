using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Common.Services;

/// <summary>
/// Used when no real email provider (e.g. SendGrid) is registered so notification handlers can still resolve <see cref="IEmailService"/>.
/// </summary>
public sealed class NullEmailService : IEmailService
{
    private readonly ILogger<NullEmailService> _logger;

    public NullEmailService(ILogger<NullEmailService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        _logger.LogDebug(
            "Email not sent (no provider configured). To: {To}, Subject: {Subject}.",
            message.To,
            message.Subject);

        return Task.CompletedTask;
    }
}
