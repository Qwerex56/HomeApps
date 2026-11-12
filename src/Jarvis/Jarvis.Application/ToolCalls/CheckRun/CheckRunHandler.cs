using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Jarvis.Application.ToolCalls.CheckRun;

internal sealed class CheckRunHandler(RunAccess access) : IQueryHandler<CheckRunQuery, Guid>
{
    public async Task<Result<Guid>> Handle(CheckRunQuery query, CancellationToken cancellationToken) =>
        (await access.RunningAsync(query.RunId, query.UserId, cancellationToken)).Map(run => run.Id);
}
