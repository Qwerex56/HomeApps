using HomeApps.Contracts;

namespace HomeApps.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Publishes integration messages to other services. Messages are written to the outbox and sent only if the
/// unit of work commits, so call it from a command handler or a domain event handler, never after saving.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IIntegrationEvent;
}
