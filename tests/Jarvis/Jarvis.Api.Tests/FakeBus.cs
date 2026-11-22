using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts;

namespace Jarvis.Api.Tests;

internal sealed class FakeBus : IEventBus
{
    public List<IIntegrationEvent> Published { get; } = [];

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IIntegrationEvent
    {
        Published.Add(message);
        return Task.CompletedTask;
    }
}
