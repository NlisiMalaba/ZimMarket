using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;

namespace ZimMarket.Application.Common.Services;

/// <summary>
/// Used when no push provider is configured so handlers can resolve <see cref="IPushNotificationService"/> safely.
/// </summary>
public sealed class NullPushNotificationService : IPushNotificationService
{
    private readonly ILogger<NullPushNotificationService> _logger;

    public NullPushNotificationService(ILogger<NullPushNotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendAsync(
        string token,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Push not sent (no push provider configured). TokenLength={TokenLength}, Title={Title}.",
            token?.Length ?? 0,
            title);

        return Task.CompletedTask;
    }

    public Task SendToTopicAsync(
        string topic,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Topic push not sent (no push provider configured). Topic={Topic}, Title={Title}.",
            topic,
            title);

        return Task.CompletedTask;
    }
}