using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Infrastructure.Configuration;

namespace ZimMarket.Infrastructure.Notifications;

/// <summary>
/// Firebase Cloud Messaging via the Firebase Admin SDK (<see cref="FirebaseMessaging"/>).
/// </summary>
public sealed class FcmPushNotificationService : IPushNotificationService
{
    private static readonly object InitLock = new();

    private readonly ILogger<FcmPushNotificationService> _logger;

    public FcmPushNotificationService(
        IOptions<FirebaseAdminOptions> options,
        ILogger<FcmPushNotificationService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        EnsureFirebaseApp(options.Value);
    }

    /// <inheritdoc />
    public async Task SendAsync(
        string token,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("FCM device token is required.", nameof(token));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Notification title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Notification body is required.", nameof(body));

        var message = new Message
        {
            Token = token.Trim(),
            Notification = new Notification { Title = title.Trim(), Body = body.Trim() },
            Data = NormalizeData(data)
        };

        try
        {
            string messageId = await FirebaseMessaging.DefaultInstance
                .SendAsync(message, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("FCM message sent to device. MessageId {MessageId}.", messageId);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogWarning(ex, "FCM send to device failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SendToTopicAsync(
        string topic,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("FCM topic is required.", nameof(topic));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Notification title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Notification body is required.", nameof(body));

        string topicName = topic.Trim();
        if (topicName.StartsWith("/topics/", StringComparison.Ordinal))
            topicName = topicName["/topics/".Length..];

        var message = new Message
        {
            Topic = topicName,
            Notification = new Notification { Title = title.Trim(), Body = body.Trim() },
            Data = NormalizeData(data)
        };

        try
        {
            string messageId = await FirebaseMessaging.DefaultInstance
                .SendAsync(message, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("FCM message sent to topic {Topic}. MessageId {MessageId}.", topicName, messageId);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogWarning(ex, "FCM send to topic failed.");
            throw;
        }
    }

    private static void EnsureFirebaseApp(FirebaseAdminOptions options)
    {
        if (FirebaseApp.DefaultInstance is not null)
            return;

        lock (InitLock)
        {
            if (FirebaseApp.DefaultInstance is not null)
                return;

            GoogleCredential credential = CreateCredential(options);
            var appOptions = new AppOptions { Credential = credential };
            if (!string.IsNullOrWhiteSpace(options.ProjectId))
                appOptions.ProjectId = options.ProjectId.Trim();

            FirebaseApp.Create(appOptions);
        }
    }

    private static GoogleCredential CreateCredential(FirebaseAdminOptions options)
    {
        if (options.UseApplicationDefaultCredentials)
            return GoogleCredential.GetApplicationDefault();

        if (!string.IsNullOrWhiteSpace(options.CredentialsJson))
            return CredentialFactory.FromJson<ServiceAccountCredential>(options.CredentialsJson).ToGoogleCredential();

        if (!string.IsNullOrWhiteSpace(options.CredentialsPath))
            return CredentialFactory.FromFile<ServiceAccountCredential>(options.CredentialsPath).ToGoogleCredential();

        throw new InvalidOperationException(
            "Firebase credentials are not configured. Set CredentialsJson, CredentialsPath, or UseApplicationDefaultCredentials.");
    }

    /// <summary>FCM data payloads require string values only.</summary>
    private static IReadOnlyDictionary<string, string>? NormalizeData(IReadOnlyDictionary<string, string>? data)
    {
        if (data is null || data.Count == 0)
            return null;

        var copy = new Dictionary<string, string>(data.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> pair in data)
        {
            if (string.IsNullOrEmpty(pair.Key))
                continue;

            copy[pair.Key] = pair.Value ?? string.Empty;
        }

        return copy.Count == 0 ? null : copy;
    }
}
