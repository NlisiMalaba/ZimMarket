using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Common.Interfaces;

public interface INotificationJobScheduler
{
    void EnqueueEmail(EmailMessage message);

    void EnqueueSms(string to, string message);

    void EnqueuePushToToken(
        string token,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null);

    void EnqueuePushToTopic(
        string topic,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null);
}