using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Installations.GetMyAccess;

/// <summary>What the caller may do in an installation: their role and the add-on's permissions they have.</summary>
public sealed record GetMyAccessQuery(Guid InstallationId) : IQuery<MyAccessView>;
