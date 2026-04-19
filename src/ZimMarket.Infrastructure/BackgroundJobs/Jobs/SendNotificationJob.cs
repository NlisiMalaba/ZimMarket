using Hangfire;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Generic fire-and-forget notification delivery job.
/// </summary>
[AutomaticRetry(Attempts = 3, DelaysInSeconds = [10, 20, 40], LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public sealed class SendNotificationJob
{
    public const string EmailDirectTemplate = "notification.email.direct";
    public const string SmsDirectTemplate = "notification.sms.direct";
    public const string PushTokenDirectTemplate = "notification.push.token.direct";
    public const string PushTopicDirectTemplate = "notification.push.topic.direct";

    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<SendNotificationJob> _logger;

    public SendNotificationJob(
        IEmailService emailService,
        ISmsService smsService,
        IPushNotificationService pushService,
        ILogger<SendNotificationJob> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _pushService = pushService ?? throw new ArgumentNullException(nameof(pushService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(SendNotificationJobPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(payload.Parameters);

        switch (payload.Channel)
        {
            case NotificationChannel.Email:
                await SendEmailAsync(payload).ConfigureAwait(false);
                break;
            case NotificationChannel.Sms:
                await SendSmsAsync(payload).ConfigureAwait(false);
                break;
            case NotificationChannel.Push:
                await SendPushAsync(payload).ConfigureAwait(false);
                break;
            default:
                throw new InvalidOperationException($"Unsupported notification channel '{payload.Channel}'.");
        }

        _logger.LogInformation(
            "SendNotificationJob delivered notification. UserId={UserId}, Channel={Channel}, TemplateId={TemplateId}.",
            payload.UserId,
            payload.Channel,
            payload.TemplateId);
    }

    private async Task SendEmailAsync(SendNotificationJobPayload payload)
    {
        EnsureTemplate(payload, EmailDirectTemplate);

        await _emailService.SendAsync(new EmailMessage
        {
            To = Required(payload, "to"),
            Subject = Required(payload, "subject"),
            Body = Required(payload, "body"),
            IsHtml = OptionalBool(payload, "isHtml")
        }).ConfigureAwait(false);
    }

    private async Task SendSmsAsync(SendNotificationJobPayload payload)
    {
        EnsureTemplate(payload, SmsDirectTemplate);
        await _smsService.SendAsync(Required(payload, "to"), Required(payload, "message")).ConfigureAwait(false);
    }

    private async Task SendPushAsync(SendNotificationJobPayload payload)
    {
        if (payload.TemplateId == PushTokenDirectTemplate)
        {
            await _pushService
                .SendAsync(
                    Required(payload, "token"),
                    Required(payload, "title"),
                    Required(payload, "body"),
                    ExtractData(payload))
                .ConfigureAwait(false);
            return;
        }

        if (payload.TemplateId == PushTopicDirectTemplate)
        {
            await _pushService
                .SendToTopicAsync(
                    Required(payload, "topic"),
                    Required(payload, "title"),
                    Required(payload, "body"),
                    ExtractData(payload))
                .ConfigureAwait(false);
            return;
        }

        throw new InvalidOperationException($"Unsupported push template '{payload.TemplateId}'.");
    }

    private static IReadOnlyDictionary<string, string>? ExtractData(SendNotificationJobPayload payload)
    {
        Dictionary<string, string> data = payload.Parameters
            .Where(pair => pair.Key.StartsWith("data:", StringComparison.Ordinal))
            .ToDictionary(
                pair => pair.Key["data:".Length..],
                pair => pair.Value,
                StringComparer.Ordinal);

        return data.Count == 0 ? null : data;
    }

    private static void EnsureTemplate(SendNotificationJobPayload payload, string expectedTemplate)
    {
        if (!string.Equals(payload.TemplateId, expectedTemplate, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported template '{payload.TemplateId}' for channel '{payload.Channel}'.");
    }

    private static string Required(SendNotificationJobPayload payload, string key)
    {
        if (!payload.Parameters.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Notification parameter '{key}' is required.");
        return value;
    }

    private static bool OptionalBool(SendNotificationJobPayload payload, string key)
    {
        if (!payload.Parameters.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
            return false;
        return bool.TryParse(value, out bool parsed) && parsed;
    }
}
