using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Common;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Application.Actions.Decide;

internal sealed class DecideHandler(IToolCallRepository calls, IToolExecutor executor, TimeProvider time)
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

        return ActionView.From(call, time.GetUtcNow());
    }
}
