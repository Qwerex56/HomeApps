using System.Reflection;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.BuildingBlocks.Domain;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeApps.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Base DbContext for every service. It is the service's <see cref="IUnitOfWork"/>:
/// <list type="number">
/// <item>Before saving, it collects domain events from tracked aggregates (<see cref="IHasDomainEvents"/>) and dispatches them in-process through MediatR.
/// Handlers may change more aggregates or publish integration messages (which land in the outbox tables).</item>
/// <item>It repeats until no new events appear, then saves everything in one transaction.</item>
/// </list>
/// It also maps the MassTransit outbox/inbox tables and applies every <see cref="IEntityTypeConfiguration{TEntity}"/>
/// from the derived context's assembly.
/// </summary>
public abstract class HomeAppsDbContext(DbContextOptions options, IPublisher publisher) : DbContext(options), IUnitOfWork
{
    private const int MaxDispatchRounds = 10;

    private static readonly MethodInfo PublishDomainEventMethod =
        typeof(HomeAppsDbContext).GetMethod(nameof(PublishDomainEventAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new NotSupportedException("Use SaveChangesAsync so domain events can be dispatched.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        for (var round = 0; round < MaxDispatchRounds; round++)
        {
            var aggregates = ChangeTracker
                .Entries<IHasDomainEvents>()
                .Select(e => e.Entity)
                .Where(a => a.DomainEvents.Count > 0)
                .ToList();

            if (aggregates.Count == 0)
            {
                return;
            }

            var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
            aggregates.ForEach(a => a.ClearDomainEvents());

            foreach (var domainEvent in events)
            {
                var publish = PublishDomainEventMethod.MakeGenericMethod(domainEvent.GetType());
                await (Task)publish.Invoke(null, [publisher, domainEvent, cancellationToken])!;
            }
        }

        throw new InvalidOperationException(
            $"Domain events were still being raised after {MaxDispatchRounds} rounds. Check for event handlers that trigger each other.");
    }

    private static Task PublishDomainEventAsync<TDomainEvent>(IPublisher publisher, TDomainEvent domainEvent, CancellationToken cancellationToken)
        where TDomainEvent : IDomainEvent =>
        publisher.Publish(new DomainEventNotification<TDomainEvent>(domainEvent), cancellationToken);
}
