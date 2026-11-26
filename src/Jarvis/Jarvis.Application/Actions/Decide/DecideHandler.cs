using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Assistant;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Common;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Application.Actions.Decide;

internal sealed class DecideHandler(IToolCallRepository calls, IRunRepository runs, IToolExecutor executor, IEventBus events, TimeProvider time)
    : ICommandHandler<DecideCommand, ActionView>
{
    public async Task<Result<ActionView>> Handle(DecideCommand command, CancellationToken cancellationToken)
    {
        var call = await calls.FindAsync(command.ActionId, cancellationToken);
        if (call is null)
        {
            return Result.Fail(ToolCallErrors.NotFound);
        }

        var allowed = call.CanDecide(command.UserId, time);
        if (allowed.IsFailed)
        {
            return allowed;
        }

        if (command.Approve)
        {
            var outcome = await executor.CallAsync(command.UserId, call.Tool, call.Arguments, cancellationToken);
            call.Complete(outcome.Text, outcome.Failed);
        }
        else
        {
            call.Deny();
        }

        var view = ActionView.From(call, time.GetUtcNow());

        // A decision taken anywhere (the JARVIS page, MCP, a channel's card) tells the channel the run came from.
        if (call.RunId != Guid.Empty)
        {
            var run = await runs.FindAsync(call.RunId, cancellationToken);
            if (run is not null && !string.IsNullOrWhiteSpace(run.Channel) && run.Reference is not null)
            {
                var action = Cards.Action(view);
                await events.PublishAsync(
                    new AssistantResponded(call.Id, run.Channel, run.Reference, action.Description, [action]),
                    cancellationToken);
            }
        }

        return view;
    }
}
