using Accounts.Application.Events;
using Accounts.Domain.Users.Events;
using HomeApps.BuildingBlocks.Application.Messaging;
using Contracts = HomeApps.Contracts.Accounts;

namespace Accounts.Application.Tests;

public sealed class EventTests
{
    private readonly FakeEventBus _bus = new();

    private PublishIntegrationEvents Handler => new(_bus);

    [Fact]
    public async Task EveryDomainEventOthersNeedBecomesAContract()
    {
        var id = Guid.NewGuid();
        var avatar = new Uri("https://example.com/a.png");

        await Handler.Handle(new DomainEventNotification<UserRegistered>(new(id, "Adam", avatar)), default);
        await Handler.Handle(new DomainEventNotification<UserProfileUpdated>(new(id, "Adam N.", null)), default);
        await Handler.Handle(new DomainEventNotification<UserDisabled>(new(id, true)), default);
        await Handler.Handle(new DomainEventNotification<UserEnabled>(new(id)), default);
        await Handler.Handle(new DomainEventNotification<UserErased>(new(id)), default);

        Assert.Equal(
            [
                new Contracts.UserCreated(id, "Adam", "https://example.com/a.png"),
                new Contracts.UserProfileUpdated(id, "Adam N.", null),
                new Contracts.UserDisabled(id, true),
                new Contracts.UserEnabled(id),
                new Contracts.UserErased(id),
            ],
            _bus.Published);
    }
}
