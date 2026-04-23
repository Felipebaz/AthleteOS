namespace AthleteOS.BuildingBlocks.Application.Abstractions;

public interface IIntegrationEventBus
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
