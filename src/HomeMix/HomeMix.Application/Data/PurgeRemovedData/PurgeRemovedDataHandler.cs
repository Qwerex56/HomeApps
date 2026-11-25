using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;

namespace HomeMix.Application.Data.PurgeRemovedData;

internal sealed class PurgeRemovedDataHandler(IDocumentRepository documents, TimeProvider time) : ICommandHandler<PurgeRemovedDataCommand, int>
{
    public async Task<Result<int>> Handle(PurgeRemovedDataCommand command, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var unreachable = await documents.DeleteUnreachableAsync(now - DataRetention.AfterRemoval, cancellationToken);
        return unreachable + await documents.DeleteExpiredAsync(now, cancellationToken);
    }
}
