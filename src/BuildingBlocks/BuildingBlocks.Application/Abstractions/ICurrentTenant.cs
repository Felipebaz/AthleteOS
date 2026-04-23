namespace AthleteOS.BuildingBlocks.Application.Abstractions;

public interface ICurrentTenant
{
    Guid TenantId { get; }
    bool IsAuthenticated { get; }
}
