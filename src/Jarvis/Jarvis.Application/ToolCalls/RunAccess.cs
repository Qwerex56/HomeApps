using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using Jarvis.Application.Abstractions;
using Jarvis.Domain.Runs;

namespace Jarvis.Application.ToolCalls;

/// <summary>A run the gateway may log into: the caller's own, still running. Anything else looks like no run at all.</summary>
internal sealed class RunAccess(IRunRepository runs)
{
    public static Error NotFound => new AppError("jarvis.run_not_found", "There is no such run going on.", ErrorKind.NotFound);

    public async Task<Result<AssistantRun>> RunningAsync(Guid runId, Guid userId, CancellationToken cancellationToken)
    {
        var run = await runs.FindAsync(runId, cancellationToken);
        return run is { IsRunning: true } && run.UserId == userId ? run : Result.Fail(NotFound);
    }
}
