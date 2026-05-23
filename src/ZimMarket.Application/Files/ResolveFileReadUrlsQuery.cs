using ZimMarket.Application.Common.Abstractions;

namespace ZimMarket.Application.Files;

public sealed record ResolveFileReadUrlsQuery(IReadOnlyList<string> Keys) : IQuery<IReadOnlyList<FileReadUrlDto>>;
