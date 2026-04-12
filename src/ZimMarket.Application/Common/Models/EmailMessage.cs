namespace ZimMarket.Application.Common.Models;

public sealed record EmailMessage
{
    public required string To { get; init; }

    public required string Subject { get; init; }

    public required string Body { get; init; }

    public bool IsHtml { get; init; }

    public string? ReplyTo { get; init; }

    public IReadOnlyList<string>? Cc { get; init; }

    public IReadOnlyList<string>? Bcc { get; init; }
}
