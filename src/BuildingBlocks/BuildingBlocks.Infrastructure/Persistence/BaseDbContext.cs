using AthleteOS.BuildingBlocks.Application.Abstractions;
using AthleteOS.BuildingBlocks.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;

namespace AthleteOS.BuildingBlocks.Infrastructure.Persistence;

public abstract class BaseDbContext(DbContextOptions options) : DbContext(options), IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null) return;
        await _currentTransaction.CommitAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null) return;
        await _currentTransaction.RollbackAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    protected void AddOutboxMessage(object integrationEvent)
    {
        var message = new OutboxMessage
        {
            Type = integrationEvent.GetType().AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
        };
        OutboxMessages.Add(message);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages");
            b.HasKey(m => m.Id);
            b.Property(m => m.Type).HasMaxLength(500).IsRequired();
            b.Property(m => m.Payload).IsRequired();
        });
    }
}
