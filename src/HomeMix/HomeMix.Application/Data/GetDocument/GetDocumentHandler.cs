using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Data;

namespace HomeMix.Application.Data.GetDocument;

internal sealed class GetDocumentHandler(DataAccess access, IDocumentRepository documents) : IQueryHandler<GetDocumentQuery, DocumentView>
{
    public async Task<Result<DocumentView>> Handle(GetDocumentQuery query, CancellationToken cancellationToken)
    {
        var space = await access.OpenAsync(query.InstallationId, query.Scope, query.Collection, write: false, cancellationToken);
        if (space.IsFailed)
        {
            return space.ToResult();
        }

        var document = await documents.FindAsync(space.Value, query.Collection, query.Key, cancellationToken);
        return document is null ? Result.Fail(DataErrors.NotFound) : DocumentView.From(document);
    }
}
