using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Application.ToolCalls.RecordCall;

internal sealed class RecordCallHandler(RunAccess access, IToolCallRepository calls, TimeProvider time) : ICommandHandler<RecordCallCommand>
{
    public async Task<Result> Handle(RecordCallCommand command, CancellationToken cancellationToken)
    {
        var run = await access.RunningAsync(command.RunId, command.UserId, cancellationToken);
        if (run.IsFailed)
        {
            return run.ToResult();
        }

        calls.Add(ToolCall.Record(command.RunId, command.UserId, command.Tool, command.Arguments, command.Result, command.Failed, command.ReadOnly, time));
        return Result.Ok();
    }
}
