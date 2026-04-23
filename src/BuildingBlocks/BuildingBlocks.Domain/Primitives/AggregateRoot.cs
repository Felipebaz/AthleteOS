using AthleteOS.BuildingBlocks.Domain.Events;

namespace AthleteOS.BuildingBlocks.Domain.Primitives;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRootWithEvents
    where TId : struct
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id) { }

    // Required by EF Core.
    protected AggregateRoot() { }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
