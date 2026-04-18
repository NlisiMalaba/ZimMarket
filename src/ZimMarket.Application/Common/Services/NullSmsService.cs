using Microsoft.Extensions.Logging;
using ZimMarket.Application.Common.Interfaces;

namespace ZimMarket.Application.Common.Services;

/// <summary>
/// Used when Twilio is not configured so handlers can resolve <see cref="ISmsService"/> without sending SMS.
/// </summary>
public sealed class NullSmsService : ISmsService
{
    private readonly ILogger<NullSmsService> _logger;

    public NullSmsService(ILogger<NullSmsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "SMS not sent (no SMS provider configured). To: {To}, Length: {Length}.",
            to,
            message?.Length ?? 0);

        return Task.CompletedTask;
    }
}
