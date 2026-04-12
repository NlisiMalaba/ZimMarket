namespace ZimMarket.Application.Common.Interfaces;

public interface IPushNotificationService
{
    Task SendAsync(
        string token,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken cancellationToken = default);

    Task SendToTopicAsync(
        string topic,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken cancellationToken = default);
}
