using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Assistant;
using Jarvis.Application.Actions.Decide;
using MassTransit;
using MediatR;

namespace Jarvis.Infrastructure.Messaging;

/// <summary>A click on a channel's card (JV9): decided, and the card's new state goes back to that channel.</summary>
public sealed class AssistantConfirmedConsumer(ISender sender, IEventBus events) : IConsumer<AssistantConfirmed>
{
    public async Task Consume(ConsumeContext<AssistantConfirmed> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var confirmed = context.Message;
        var decided = await sender.Send(new DecideCommand(confirmed.UserId, confirmed.ConfirmationId, confirmed.Approved), context.CancellationToken);
        if (decided.IsFailed)
        {
            // Not theirs, expired or already decided: the card keeps what it shows; an expired one says so by its time.
            return;
        }

        var action = Cards.Action(decided.Value);
        await events.PublishAsync(
            new AssistantResponded(confirmed.ConfirmationId, confirmed.Channel, confirmed.Reference, action.Description, [action]),
            context.CancellationToken);
    }
}
