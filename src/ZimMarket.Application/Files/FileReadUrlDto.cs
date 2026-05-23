namespace ZimMarket.Application.Files;

public sealed record FileReadUrlDto(string Key, string Url, DateTimeOffset ExpiresAt);
