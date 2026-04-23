namespace AthleteOS.BuildingBlocks.Application.Abstractions;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    string EventType { get; }
}
