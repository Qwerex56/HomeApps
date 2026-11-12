using System.Text.Json;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Domain.Data;

namespace HomeMix.Application.Data.PutDocument;

/// <summary>Creates or replaces a document.</summary>
/// <param name="Version">The version the writer read; a stale one answers 409 <c>homemix.conflict</c>. Without it the write wins.</param>
public sealed record PutDocumentCommand(Guid InstallationId, DataScope Scope, string Collection, string Key, JsonElement Value, int? Version)
    : ICommand<DocumentView>;
