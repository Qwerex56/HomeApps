using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Domain.Data;

namespace HomeMix.Application.Data.GetDocument;

public sealed record GetDocumentQuery(Guid InstallationId, DataScope Scope, string Collection, string Key) : IQuery<DocumentView>;
