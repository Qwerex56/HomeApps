using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Common;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Application.ToolCalls.RecordCall;

internal sealed class RecordCallHandler(RunAccess access, IToolCallRepository calls, IRunFeed feed, TimeProvider time)
    : ICommandHandler<RecordCallCommand>
{
    public async Task<Result> Handle(RecordCallCommand command, CancellationToken cancellationToken)
    {
        var run = await access.RunningAsync(command.RunId, command.UserId, cancellationToken);
        if (run.IsFailed)
        {
            return run.ToResult();
        }

        var call = ToolCall.Record(command.RunId, command.UserId, command.Tool, command.Arguments, command.Result, command.Failed, command.ReadOnly, time);
        calls.Add(call);
        if (!call.ReadOnly)
        {
            feed.Publish(call.RunId, new RunAction(ActionView.From(call, time.GetUtcNow())));
        }

        return Result.Ok();
    }
}
