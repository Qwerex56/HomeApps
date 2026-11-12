using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Data;

namespace HomeMix.Application.Data.DeleteDocument;

internal sealed class DeleteDocumentHandler(DataAccess access, IDocumentRepository documents) : ICommandHandler<DeleteDocumentCommand>
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
        return Result.Ok();
    }
}
