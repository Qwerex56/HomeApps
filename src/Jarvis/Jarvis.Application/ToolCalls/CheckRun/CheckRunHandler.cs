using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Jarvis.Application.ToolCalls.CheckRun;

internal sealed class CheckRunHandler(RunAccess access) : IQueryHandler<CheckRunQuery, RunScope>
{
    public async Task<Result<RunScope>> Handle(CheckRunQuery query, CancellationToken cancellationToken) =>
        (await access.RunningAsync(query.RunId, query.UserId, cancellationToken)).Map(run => new RunScope(run.Id, run.HouseholdId));
}
