using HomeApps.BuildingBlocks.Api.Assistant;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeMix.Api.Requests;
using HomeMix.Application.Data;
using HomeMix.Application.Data.DeleteDocument;
using HomeMix.Application.Data.GetDocument;
using HomeMix.Application.Data.ListDocuments;
using HomeMix.Application.Data.PutDocument;
using HomeMix.Domain.Data;
using MediatR;

namespace HomeMix.Api.Endpoints;

/// <summary>
/// An installation's data (docs/homemix.md, "Data"), called by the web app's bridge with the person's token, never by
/// the frame. These are the add-on's data, not HomeApps', so an assistant acting for the person may change them too.
/// <c>scope</c> is <c>household</c> (the default) or <c>user</c>.
/// </summary>
internal sealed class DataEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var data = app.MapGroup("/homemix/api/installations/{id:guid}/data/{collection}").WithTags("Data").AllowAssistant();

        data.MapGet("/", async (Guid id, string collection, string? scope, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListDocumentsQuery(id, Scope(scope), collection), ct)).ToHttpResult())
            .Produces<IReadOnlyList<DocumentView>>();

        data.MapGet("/{key}", async (Guid id, string collection, string key, string? scope, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetDocumentQuery(id, Scope(scope), collection, key), ct)).ToHttpResult())
            .Produces<DocumentView>();

        data.MapPut("/{key}", async (Guid id, string collection, string key, string? scope, PutDocumentRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new PutDocumentCommand(id, Scope(scope), collection, key, body.Value, body.Version), ct))
                .ToHttpResult())
            .Produces<DocumentView>();

        data.MapDelete("/{key}", async (Guid id, string collection, string key, string? scope, int? version, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DeleteDocumentCommand(id, Scope(scope), collection, key, version), ct)).ToHttpResult());
    }

    /// <summary><c>household</c> when left out; anything unknown fails validation (a value outside the enum).</summary>
    private static DataScope Scope(string? scope) =>
        string.IsNullOrEmpty(scope) ? DataScope.Household
        : Enum.TryParse<DataScope>(scope, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed
        : (DataScope)(-1);
}
