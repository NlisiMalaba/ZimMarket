using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using ZimMarket.Application.Common.Interfaces;
using ZimMarket.Infrastructure.Configuration;

namespace ZimMarket.Infrastructure.Notifications;

public sealed class TwilioSmsService : ISmsService
{
    private const int MaxSmsLength = 1600;

    private readonly TwilioRestClient _client;
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioSmsService> _logger;

    public TwilioSmsService(IOptions<TwilioOptions> options, ILogger<TwilioSmsService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value;
        _client = new TwilioRestClient(_options.AccountSid, _options.AuthToken);
    }

    /// <inheritdoc />
    public async Task SendAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("SMS recipient is required.", nameof(to));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("SMS message is required.", nameof(message));

        string body = message.Trim();
        if (body.Length > MaxSmsLength)
        {
            throw new ArgumentException(
                $"SMS message exceeds maximum length ({MaxSmsLength} characters).",
                nameof(message));
        }

        MessageResource sent = await MessageResource.CreateAsync(
                body: body,
                from: new PhoneNumber(_options.FromPhoneNumber),
                to: new PhoneNumber(to.Trim()),
                client: _client)
            .ConfigureAwait(false);

        _logger.LogInformation("SMS sent via Twilio. MessageSid {MessageSid}.", sent.Sid);
    }
}
