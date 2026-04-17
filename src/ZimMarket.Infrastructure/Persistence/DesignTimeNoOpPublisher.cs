using MediatR;

namespace ZimMarket.Infrastructure.Persistence;

/// <summary>
/// MediatR publisher used only at design time so <see cref="AppDbContext"/> can be constructed for EF tooling.
/// </summary>
internal sealed class DesignTimeNoOpPublisher : IPublisher
{
    public Task Publish(object notification, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification =>
        Task.CompletedTask;
}
