using Hangfire;
using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Application.Common.Models;
using ZimMarket.Infrastructure.BackgroundJobs.Jobs;

namespace ZimMarket.Infrastructure.BackgroundJobs;

/// <summary>
/// Schedules notification deliveries as Hangfire fire-and-forget jobs.
/// </summary>
public sealed class HangfireNotificationJobScheduler : INotificationJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobs;

    public HangfireNotificationJobScheduler(
        IBackgroundJobClient backgroundJobs)
    {
        _backgroundJobs = backgroundJobs ?? throw new ArgumentNullException(nameof(backgroundJobs));
    }

    public void EnqueueEmail(EmailMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var payload = new SendNotificationJobPayload
        {
            UserId = Guid.Empty,
            Channel = NotificationChannel.Email,
            TemplateId = SendNotificationJob.EmailDirectTemplate,
            Parameters = new Dictionary<string, string>
            {
                ["to"] = message.To,
                ["subject"] = message.Subject,
                ["body"] = message.Body,
                ["isHtml"] = message.IsHtml.ToString()
            }
        };

        _backgroundJobs.Enqueue<SendNotificationJob>(x => x.ExecuteAsync(payload));
    }

    public void EnqueueSms(string to, string message)
    {
        var payload = new SendNotificationJobPayload
        {
            UserId = Guid.Empty,
            Channel = NotificationChannel.Sms,
            TemplateId = SendNotificationJob.SmsDirectTemplate,
            Parameters = new Dictionary<string, string>
            {
                ["to"] = to,
                ["message"] = message
            }
        };

        _backgroundJobs.Enqueue<SendNotificationJob>(x => x.ExecuteAsync(payload));
    }

    public void EnqueuePushToToken(string token, string title, string body, IReadOnlyDictionary<string, string>? data = null)
    {
        var parameters = new Dictionary<string, string>
        {
            ["token"] = token,
            ["title"] = title,
            ["body"] = body
        };

        if (data is not null)
        {
            foreach (var pair in data)
                parameters[$"data:{pair.Key}"] = pair.Value;
        }

        var payload = new SendNotificationJobPayload
        {
            UserId = Guid.Empty,
            Channel = NotificationChannel.Push,
            TemplateId = SendNotificationJob.PushTokenDirectTemplate,
            Parameters = parameters
        };

        _backgroundJobs.Enqueue<SendNotificationJob>(x => x.ExecuteAsync(payload));
    }

    public void EnqueuePushToTopic(string topic, string title, string body, IReadOnlyDictionary<string, string>? data = null)
    {
        var parameters = new Dictionary<string, string>
        {
            ["topic"] = topic,
            ["title"] = title,
            ["body"] = body
        };

        if (data is not null)
        {
            foreach (var pair in data)
                parameters[$"data:{pair.Key}"] = pair.Value;
        }

        var payload = new SendNotificationJobPayload
        {
            UserId = Guid.Empty,
            Channel = NotificationChannel.Push,
            TemplateId = SendNotificationJob.PushTopicDirectTemplate,
            Parameters = parameters
        };

        _backgroundJobs.Enqueue<SendNotificationJob>(x => x.ExecuteAsync(payload));
    }
}