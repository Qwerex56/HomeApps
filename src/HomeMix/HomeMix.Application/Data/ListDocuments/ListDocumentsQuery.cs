using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Domain.Data;

namespace HomeMix.Application.Data.ListDocuments;

/// <summary>Every document of a collection in the caller's view of an installation's space.</summary>
public sealed record ListDocumentsQuery(Guid InstallationId, DataScope Scope, string Collection) : IQuery<IReadOnlyList<DocumentView>>;
