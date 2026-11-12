using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;

namespace HomeMix.Application.Data.ListDocuments;

internal sealed class ListDocumentsHandler(DataAccess access, IDocumentRepository documents)
    : IQueryHandler<ListDocumentsQuery, IReadOnlyList<DocumentView>>
{
    public async Task<Result<IReadOnlyList<DocumentView>>> Handle(ListDocumentsQuery query, CancellationToken cancellationToken)
    {
        var space = await access.OpenAsync(query.InstallationId, query.Scope, query.Collection, write: false, cancellationToken);
        if (space.IsFailed)
        {
            return space.ToResult();
        }

        var list = await documents.ListAsync(space.Value, query.Collection, cancellationToken);
        return Result.Ok<IReadOnlyList<DocumentView>>(list.Select(DocumentView.From).ToList());
    }
}
