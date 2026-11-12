using HomeApps.Contracts.Assistant;
using HomeApps.BuildingBlocks.Application.Abstractions;
using MassTransit;

namespace Jarvis.Infrastructure.Messaging;

/// <summary>
/// A channel asks (the chat's <c>@Jarvis</c>, J3): "working" at once; <see cref="InvocationWorker"/> then runs it and
/// answers with <see cref="AssistantResponded"/>.
/// </summary>
public sealed class AssistantInvokedConsumer(InvocationQueue queue, IEventBus events) : IConsumer<AssistantInvoked>
{
    public async Task Consume(ConsumeContext<AssistantInvoked> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var asked = context.Message;
        await events.PublishAsync(new AssistantWorking(asked.InvocationId, asked.Channel, asked.Reference), context.CancellationToken);
        await queue.EnqueueAsync(asked, context.CancellationToken);
    }
}
