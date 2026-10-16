using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts;
using MassTransit;

namespace HomeApps.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// <see cref="IEventBus"/> on top of MassTransit. With the bus outbox enabled, the scoped <see cref="IPublishEndpoint"/>
/// writes to the outbox table of the service's DbContext, so the message is sent only after the unit of work commits.
/// </summary>
internal sealed class MassTransitEventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IIntegrationEvent =>
        publishEndpoint.Publish(message, cancellationToken);
}
