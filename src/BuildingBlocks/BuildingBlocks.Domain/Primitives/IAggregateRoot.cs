using AthleteOS.BuildingBlocks.Domain.Events;

namespace AthleteOS.BuildingBlocks.Domain.Primitives;

public interface IAggregateRootWithEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
