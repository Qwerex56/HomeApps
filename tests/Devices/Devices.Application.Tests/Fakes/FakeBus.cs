using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts;

namespace Devices.Application.Tests.Fakes;

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
