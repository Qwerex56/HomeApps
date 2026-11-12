using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Common;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Application.ToolCalls.AwaitConfirmation;

internal sealed class AwaitConfirmationHandler(RunAccess access, IToolCallRepository calls, IRunFeed feed, TimeProvider time)
    : ICommandHandler<AwaitConfirmationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AwaitConfirmationCommand command, CancellationToken cancellationToken)
    {
        var run = await access.RunningAsync(command.RunId, command.UserId, cancellationToken);
        if (run.IsFailed)
        {
            return run.ToResult<Guid>();
        }

        var call = ToolCall.AwaitConfirmation(command.RunId, command.UserId, command.Tool, command.Arguments, time);
        calls.Add(call);
        feed.Publish(call.RunId, new RunAction(ActionView.From(call, time.GetUtcNow())));
        return call.Id;
    }
}
