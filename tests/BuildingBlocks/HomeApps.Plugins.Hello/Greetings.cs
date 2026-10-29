using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Plugins.Hosting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeApps.Plugins.Hello;

public sealed class Greeting
{
    private Greeting(Guid id, Guid householdId, string text, Guid? by)
    {
        Id = id;
        HouseholdId = householdId;
        Text = text;
        By = by;
    }

    public Guid Id { get; private set; }

    public Guid HouseholdId { get; private set; }

    public string Text { get; private set; }

    /// <summary><c>null</c> for the greeting the plugin adds when it is first switched on.</summary>
    public Guid? By { get; private set; }

    public static Greeting Say(Guid householdId, string text, Guid? by) => new(Guid.NewGuid(), householdId, text, by);
}

public sealed class HelloDbContext(DbContextOptions<HelloDbContext> options, IPublisher publisher) : PluginDbContext(options, publisher)
{
    public DbSet<Greeting> Greetings => Set<Greeting>();
}

public sealed record SayHelloCommand(Guid HouseholdId, string Text) : ICommand<Guid>, IHouseholdRequest
{
    public string Permission => HelloPlugin.Write;
}

internal sealed class SayHelloValidator : AbstractValidator<SayHelloCommand>
{
    public SayHelloValidator() => RuleFor(c => c.Text).NotEmpty().MaximumLength(100);
}

internal sealed class SayHelloHandler(HelloDbContext db, ICurrentUser user) : ICommandHandler<SayHelloCommand, Guid>
{
    public Task<Result<Guid>> Handle(SayHelloCommand command, CancellationToken cancellationToken)
    {
        var greeting = Greeting.Say(command.HouseholdId, command.Text, user.RequiredUserId);
        db.Greetings.Add(greeting);
        return Task.FromResult(Result.Ok(greeting.Id));
    }
}

public sealed record GreetingView(Guid Id, string Text, Guid? By);

public sealed record ListGreetingsQuery(Guid HouseholdId) : IQuery<IReadOnlyList<GreetingView>>, IHouseholdRequest
{
    public string Permission => HelloPlugin.Read;
}

internal sealed class ListGreetingsHandler(HelloDbContext db) : IQueryHandler<ListGreetingsQuery, IReadOnlyList<GreetingView>>
{
    public async Task<Result<IReadOnlyList<GreetingView>>> Handle(ListGreetingsQuery query, CancellationToken cancellationToken) =>
        await db.Greetings.Where(g => g.HouseholdId == query.HouseholdId)
            .Select(g => new GreetingView(g.Id, g.Text, g.By))
            .ToListAsync(cancellationToken);
}

/// <summary>The seeding hook: the first switch-on in a household adds a greeting in the same transaction.</summary>
internal sealed class SeedOnFirstSwitchOn(HelloDbContext db) : INotificationHandler<DomainEventNotification<PluginSwitchedOn>>
{
    public Task Handle(DomainEventNotification<PluginSwitchedOn> notification, CancellationToken cancellationToken)
    {
        if (notification.DomainEvent.FirstTime)
        {
            db.Greetings.Add(Greeting.Say(notification.DomainEvent.HouseholdId, "Cześć!", null));
        }

        return Task.CompletedTask;
    }
}
