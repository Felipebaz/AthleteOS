using AthleteOS.BuildingBlocks.Domain.Events;
using MediatR;

namespace AthleteOS.BuildingBlocks.Application.Messaging;

/// <summary>Implement to handle a specific domain event within the same bounded context.</summary>
public interface IDomainEventListener<TDomainEvent> : INotificationHandler<DomainEventNotification<TDomainEvent>>
    where TDomainEvent : IDomainEvent { }

public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
