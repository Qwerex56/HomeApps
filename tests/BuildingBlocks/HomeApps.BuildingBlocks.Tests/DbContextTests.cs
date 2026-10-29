using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.BuildingBlocks.Domain;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeApps.BuildingBlocks.Tests;

public sealed record GadgetCreated(Guid Id) : IDomainEvent;

public sealed record GadgetAudited(Guid Id) : IDomainEvent;

public sealed class Gadget : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private Gadget() { }

    private Gadget(Guid id) => Id = id;

    public Guid Id { get; private set; }

    public bool Audited { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    public void ClearDomainEvents() => _domainEvents.Clear();

    public static Gadget Create()
    {
        var gadget = new Gadget(Guid.NewGuid());
        gadget._domainEvents.Add(new GadgetCreated(gadget.Id));
        return gadget;
    }

    public void Audit()
    {
        Audited = true;
        _domainEvents.Add(new GadgetAudited(Id));
    }
}

public sealed class GadgetDbContext(DbContextOptions<GadgetDbContext> options, IPublisher publisher)
    : HomeAppsDbContext(options, publisher)
{
    public DbSet<Gadget> Gadgets => Set<Gadget>();
}

public sealed class EventLog
{
    public List<IDomainEvent> Events { get; } = [];
}

// Handler that changes the aggregate again, which raises a second event: proves the dispatch loop.
public sealed class AuditOnCreate(GadgetDbContext db, EventLog log) : IDomainEventHandler<GadgetCreated>
{
    public async Task Handle(DomainEventNotification<GadgetCreated> notification, CancellationToken cancellationToken)
    {
        log.Events.Add(notification.DomainEvent);
        var gadget = await db.Gadgets.FindAsync([notification.DomainEvent.Id], cancellationToken);
        gadget!.Audit();
    }
}

public sealed class LogAudited(EventLog log) : IDomainEventHandler<GadgetAudited>
{
    public Task Handle(DomainEventNotification<GadgetAudited> notification, CancellationToken cancellationToken)
    {
        log.Events.Add(notification.DomainEvent);
        return Task.CompletedTask;
    }
}

public sealed class DbContextTests
{
    [Fact]
    public async Task Saving_dispatches_domain_events_until_none_are_left()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<EventLog>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DbContextTests>());
        services.AddDbContext<GadgetDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GadgetDbContext>();

        var gadget = Gadget.Create();
        db.Gadgets.Add(gadget);
        await db.SaveChangesAsync();

        var log = scope.ServiceProvider.GetRequiredService<EventLog>();
        Assert.Collection(
            log.Events,
            e => Assert.IsType<GadgetCreated>(e),
            e => Assert.IsType<GadgetAudited>(e));
        Assert.Empty(gadget.DomainEvents);
        Assert.True((await db.Gadgets.AsNoTracking().SingleAsync()).Audited);
    }
}
