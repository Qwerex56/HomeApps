using System.Threading.Channels;
using HomeApps.Contracts.Assistant;

namespace Jarvis.Infrastructure.Messaging;

/// <summary>
/// Invocations waiting for <see cref="InvocationWorker"/>. A run takes minutes and calls back into this service's own
/// gateway, so it cannot run inside the consumer: the consumer's outbox transaction would hide the run from the gateway.
/// </summary>
public sealed class InvocationQueue
{
    private readonly Channel<AssistantInvoked> _channel = Channel.CreateUnbounded<AssistantInvoked>();

    public ValueTask EnqueueAsync(AssistantInvoked invoked, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(invoked, cancellationToken);

    public IAsyncEnumerable<AssistantInvoked> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
