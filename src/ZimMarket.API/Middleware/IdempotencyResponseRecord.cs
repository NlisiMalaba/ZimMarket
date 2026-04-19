namespace ZimMarket.API.Middleware;

/// <summary>
/// Serializable HTTP snapshot stored in Redis for idempotent POST replay.
/// </summary>
internal sealed class IdempotencyResponseRecord
{
    public required int StatusCode { get; init; }

    public string? ContentType { get; init; }

    public string? Location { get; init; }

    public required string BodyBase64 { get; init; }
}
