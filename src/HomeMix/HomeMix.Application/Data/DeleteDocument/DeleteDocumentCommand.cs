using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Domain.Data;

namespace HomeMix.Application.Data.DeleteDocument;

/// <param name="Version">As for a write: a stale one answers 409, without it the delete wins.</param>
public sealed record DeleteDocumentCommand(Guid InstallationId, DataScope Scope, string Collection, string Key, int? Version) : ICommand;
