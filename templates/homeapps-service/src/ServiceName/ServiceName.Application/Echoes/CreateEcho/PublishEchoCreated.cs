using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using ServiceName.Domain.Echoes;
using Contracts = HomeApps.Contracts.ServiceName;

namespace ServiceName.Application.Echoes.CreateEcho;

/// <summary>Maps the domain event to the integration message. It lands in the outbox in the same transaction.</summary>
internal sealed class PublishEchoCreated(IEventBus eventBus) : IDomainEventHandler<EchoCreated>
{
    public Task Handle(DomainEventNotification<EchoCreated> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        return eventBus.PublishAsync(new Contracts.EchoCreated(e.EchoId, e.OwnerId, e.Message), cancellationToken);
    }
}
