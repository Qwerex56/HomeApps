using System.Collections.Concurrent;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts;
using MassTransit;

namespace HomeApps.Plugins.Tests;

/// <summary>
/// Publishes like the kit (through the outbox) and remembers what was published: the harness sees outbox deliveries
/// only as serialized bodies.
/// </summary>
public sealed class RecordingEventBus(IPublishEndpoint publishEndpoint, ConcurrentQueue<object> published) : IEventBus
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IIntegrationEvent
    {
        published.Enqueue(message);
        return publishEndpoint.Publish(message, cancellationToken);
    }
}
