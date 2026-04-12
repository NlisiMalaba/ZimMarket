using ZimMarket.Application.Common.Models;

namespace ZimMarket.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
