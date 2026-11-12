using HomeMix.Domain.Data;

namespace HomeMix.Application.Abstractions;

/// <summary>Where a document lives: an installation, a scope and, for <see cref="DataScope.User"/>, the person.</summary>
public sealed record DataSpace(Guid InstallationId, DataScope Scope, Guid? OwnerUserId);
