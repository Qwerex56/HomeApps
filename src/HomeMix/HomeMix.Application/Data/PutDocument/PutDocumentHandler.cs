using System.Text;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Data;
using Microsoft.Extensions.Options;

namespace HomeMix.Application.Data.PutDocument;

internal sealed class PutDocumentHandler(
    DataAccess access,
    IDocumentRepository documents,
    ICurrentUser currentUser,
    TimeProvider time,
    IOptions<HomeMixOptions> options) : ICommandHandler<PutDocumentCommand, DocumentView>
{
    public async Task<Result<DocumentView>> Handle(PutDocumentCommand command, CancellationToken cancellationToken)
    {
        var limits = options.Value;
        var json = command.Value.GetRawText();
        var size = Encoding.UTF8.GetByteCount(json);
        if (size > limits.MaxDocumentBytes)
        {
            return Result.Fail(DataErrors.TooLarge(limits.MaxDocumentBytes));
        }

        var space = await access.OpenAsync(command.InstallationId, command.Scope, command.Collection, write: true, cancellationToken);
        if (space.IsFailed)
        {
            return space.ToResult();
        }

        var document = await documents.FindAsync(space.Value, command.Collection, command.Key, cancellationToken);
        var used = await documents.UsedBytesAsync(command.InstallationId, cancellationToken);
        if (used - (document?.Size ?? 0) + size > limits.MaxInstallationBytes)
        {
            return Result.Fail(DataErrors.StorageFull(limits.MaxInstallationBytes));
        }

        var me = currentUser.RequiredUserId;
        var now = time.GetUtcNow();
        if (document is not null)
        {
            var replaced = document.Replace(json, size, command.Version, me, now);
            return replaced.IsFailed ? replaced : DocumentView.From(document);
        }

        // A version names something read before; it is gone (or never was).
        if (command.Version is not null)
        {
            return Result.Fail(DataErrors.Conflict);
        }

        if (await documents.CountAsync(space.Value, command.Collection, cancellationToken) >= limits.MaxDocumentsPerCollection)
        {
            return Result.Fail(DataErrors.CollectionFull(limits.MaxDocumentsPerCollection));
        }

        var created = AddonDocument.Create(
            space.Value.InstallationId, space.Value.Scope, space.Value.OwnerUserId, command.Collection, command.Key, json, size, me, now);
        documents.Add(created);
        return DocumentView.From(created);
    }
}
