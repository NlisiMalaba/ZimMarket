namespace ZimMarket.Application.Common.Models;

public sealed record SendNotificationJobPayload
{
    public required Guid UserId { get; init; }

    public required NotificationChannel Channel { get; init; }

    public required string TemplateId { get; init; }

    public required IReadOnlyDictionary<string, string> Parameters { get; init; }
}
