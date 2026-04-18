using Hangfire;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Infrastructure.BackgroundJobs;

/// <summary>
/// Schedules notification deliveries as Hangfire fire-and-forget jobs.
/// </summary>
public sealed class HangfireNotificationJobScheduler : INotificationJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<HangfireNotificationJobScheduler> _logger;

    public HangfireNotificationJobScheduler(
        IBackgroundJobClient backgroundJobs,
        IEmailService emailService,
        ISmsService smsService,
        IPushNotificationService pushService,
        ILogger<HangfireNotificationJobScheduler> logger)
    {
        _backgroundJobs = backgroundJobs ?? throw new ArgumentNullException(nameof(backgroundJobs));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _pushService = pushService ?? throw new ArgumentNullException(nameof(pushService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void EnqueueEmail(EmailMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _backgroundJobs.Enqueue<HangfireNotificationJobScheduler>(x => x.ProcessEmailAsync(message));
    }

    public void EnqueueSms(string to, string message)
    {
        _backgroundJobs.Enqueue<HangfireNotificationJobScheduler>(x => x.ProcessSmsAsync(to, message));
    }

    public void EnqueuePushToToken(string token, string title, string body, IReadOnlyDictionary<string, string>? data = null)
    {
        _backgroundJobs.Enqueue<HangfireNotificationJobScheduler>(x => x.ProcessPushToTokenAsync(token, title, body, data));
    }

    public void EnqueuePushToTopic(string topic, string title, string body, IReadOnlyDictionary<string, string>? data = null)
    {
        _backgroundJobs.Enqueue<HangfireNotificationJobScheduler>(x => x.ProcessPushToTopicAsync(topic, title, body, data));
    }

    public async Task ProcessEmailAsync(EmailMessage message)
    {
        try
        {
            await _emailService.SendAsync(message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hangfire email dispatch failed for {To}.", message.To);
        }
    }

    public async Task ProcessSmsAsync(string to, string message)
    {
        try
        {
            await _smsService.SendAsync(to, message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hangfire SMS dispatch failed for {To}.", to);
        }
    }

    public async Task ProcessPushToTokenAsync(
        string token,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data)
    {
        try
        {
            await _pushService.SendAsync(token, title, body, data).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hangfire token push dispatch failed.");
        }
    }

    public async Task ProcessPushToTopicAsync(
        string topic,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data)
    {
        try
        {
            await _pushService.SendToTopicAsync(topic, title, body, data).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hangfire topic push dispatch failed for {Topic}.", topic);
        }
    }
}