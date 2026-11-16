using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Data;
using HomeMix.Application.Realtime;

namespace HomeMix.Application.Data.DeleteDocument;

internal sealed class DeleteDocumentHandler(DataAccess access, IDocumentRepository documents, ICurrentUser currentUser, RealtimeOutbox realtime)
    : ICommandHandler<DeleteDocumentCommand>
{
    public async Task<Result> Handle(DeleteDocumentCommand command, CancellationToken cancellationToken)
    {
        var space = await access.OpenAsync(command.InstallationId, command.Scope, command.Collection, write: true, cancellationToken);
        if (space.IsFailed)
        {
            return space.ToResult();
        }

        var document = await documents.FindAsync(space.Value, command.Collection, command.Key, cancellationToken);
        if (document is null)
        {
            return Result.Fail(DataErrors.NotFound);
        }

        if (!document.Matches(command.Version))
        {
            return Result.Fail(DataErrors.Conflict);
        }

        documents.Remove(document);
        realtime.Add(RealtimeEvents.DataChanged(
            space.Value.InstallationId, space.Value.Target, space.Value.Scope, space.Value.OwnerUserId, command.Collection, command.Key,
            currentUser.RequiredUserId));
        return Result.Ok();
    }
}
