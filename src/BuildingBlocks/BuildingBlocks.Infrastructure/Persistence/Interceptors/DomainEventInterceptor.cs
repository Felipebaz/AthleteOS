using AthleteOS.BuildingBlocks.Application.Messaging;
using AthleteOS.BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AthleteOS.BuildingBlocks.Infrastructure.Persistence.Interceptors;

public sealed class DomainEventInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(eventData, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(
        SaveChangesCompletedEventData eventData,
        CancellationToken cancellationToken)
    {
        if (eventData.Context is null) return;

        var aggregates = eventData.Context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is IAggregateRootWithEvents)
            .Select(e => (IAggregateRootWithEvents)e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            var notification = (INotification)Activator.CreateInstance(
                typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()),
                domainEvent)!;

            await publisher.Publish(notification, cancellationToken);
        }
    }
}
