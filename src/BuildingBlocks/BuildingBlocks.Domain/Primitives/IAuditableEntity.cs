namespace AthleteOS.BuildingBlocks.Domain.Primitives;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; }
    DateTime? UpdatedAt { get; }
}
