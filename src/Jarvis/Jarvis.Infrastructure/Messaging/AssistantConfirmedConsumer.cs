using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeApps.Contracts.Assistant;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Actions.Decide;
using Jarvis.Application.Common;
using Jarvis.Domain.ToolCalls;
using MassTransit;
using MediatR;

namespace Jarvis.Infrastructure.Messaging;

/// <summary>
/// A click on a channel's card (JV9): decided (the handler tells the channel the card's new state), or, when it was
/// decided or expired meanwhile, the card gets the state it really has so its buttons do not hang.
/// </summary>
public sealed class AssistantConfirmedConsumer(ISender sender, IToolCallRepository calls, IEventBus events, TimeProvider time)
    : IConsumer<AssistantConfirmed>
{
    public async Task Consume(ConsumeContext<AssistantConfirmed> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var confirmed = context.Message;
        var decided = await sender.Send(new DecideCommand(confirmed.UserId, confirmed.ConfirmationId, confirmed.Approved), context.CancellationToken);
        if (decided.IsSuccess)
        {
            return;
        }

        if (!decided.Errors.OfType<AppError>().Any(e => e.Code is "jarvis.action_not_pending" or "jarvis.action_expired"))
        {
            // Not theirs: the card keeps what it shows.
            return;
        }

        var call = await calls.FindAsync(confirmed.ConfirmationId, context.CancellationToken);
        if (call is null)
        {
            return;
        }

        var action = Cards.Action(ActionView.From(call, time.GetUtcNow()));
        await events.PublishAsync(
            new AssistantResponded(confirmed.ConfirmationId, confirmed.Channel, confirmed.Reference, action.Description, [action]),
            context.CancellationToken);
    }
}
