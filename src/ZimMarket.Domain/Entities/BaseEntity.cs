using ZimMarket.Domain.Events;

namespace ZimMarket.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; }

    public DateTimeOffset CreatedAt { get; protected set; }

    public DateTimeOffset UpdatedAt { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Returns a snapshot of pending domain events and clears the internal list.
    /// </summary>
    public IReadOnlyList<IDomainEvent> PopDomainEvents()
    {
        List<IDomainEvent> snapshot = [.. _domainEvents];
        _domainEvents.Clear();
        return snapshot;
    }
}
