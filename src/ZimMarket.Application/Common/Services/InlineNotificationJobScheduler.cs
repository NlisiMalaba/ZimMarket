using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Common.Services;

/// <summary>
/// Fallback scheduler used when Hangfire is not configured; executes notification calls inline.
/// </summary>
public sealed class InlineNotificationJobScheduler : INotificationJobScheduler
{
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<InlineNotificationJobScheduler> _logger;

    public InlineNotificationJobScheduler(
        IEmailService emailService,
        ISmsService smsService,
        IPushNotificationService pushService,
        ILogger<InlineNotificationJobScheduler> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _pushService = pushService ?? throw new ArgumentNullException(nameof(pushService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void EnqueueEmail(EmailMessage message)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendAsync(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inline email dispatch failed.");
            }
        });
    }

    public void EnqueueSms(string to, string message)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _smsService.SendAsync(to, message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inline SMS dispatch failed for {To}.", to);
            }
        });
    }

    public void EnqueuePushToToken(
        string token,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _pushService.SendAsync(token, title, body, data).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inline push dispatch to token failed.");
            }
        });
    }

    public void EnqueuePushToTopic(
        string topic,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _pushService.SendToTopicAsync(topic, title, body, data).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inline topic push dispatch failed for {Topic}.", topic);
            }
        });
    }
}