namespace ZimMarket.Application.Common.Interfaces;

public interface ISmsService
{
    Task SendAsync(string to, string message, CancellationToken cancellationToken = default);
}
