using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;

namespace Jarvis.Application.Housekeeping.ApplyRetention;

internal sealed class ApplyRetentionHandler(IRunRepository runs, TimeProvider time) : ICommandHandler<ApplyRetentionCommand>
{
    public async Task<Result> Handle(ApplyRetentionCommand command, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        await runs.ApplyRetentionAsync(now - ApplyRetentionCommand.ContentLifetime, now - ApplyRetentionCommand.RowLifetime, cancellationToken);
        return Result.Ok();
    }
}
