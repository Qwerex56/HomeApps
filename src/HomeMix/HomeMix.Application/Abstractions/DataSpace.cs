using HomeMix.Domain.Data;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Abstractions;

/// <summary>Where a document lives: an installation (and where it is installed), a scope and, for <see cref="DataScope.User"/>, the person.</summary>
public sealed record DataSpace(Guid InstallationId, DataScope Scope, Guid? OwnerUserId, InstallTarget Target);
