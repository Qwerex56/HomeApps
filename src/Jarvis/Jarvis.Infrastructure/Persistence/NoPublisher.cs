using MediatR;

namespace Jarvis.Infrastructure.Persistence;

/// <summary>For design time only: Jarvis raises no domain events yet.</summary>
internal sealed class NoPublisher : IPublisher
{
    public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification => Task.CompletedTask;
}
