using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;

namespace HomeMix.Application.Data.PurgeRemovedData;

internal sealed class PurgeRemovedDataHandler(IDocumentRepository documents, TimeProvider time) : ICommandHandler<PurgeRemovedDataCommand, int>
{
    public async Task<Result<int>> Handle(PurgeRemovedDataCommand command, CancellationToken cancellationToken) =>
        await documents.DeleteUnreachableAsync(time.GetUtcNow() - DataRetention.AfterRemoval, cancellationToken);
}
