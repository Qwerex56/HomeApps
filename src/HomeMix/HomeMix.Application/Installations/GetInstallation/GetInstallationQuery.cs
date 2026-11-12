using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Installations.GetInstallation;

/// <summary>One installation, for whoever may use it.</summary>
public sealed record GetInstallationQuery(Guid InstallationId) : IQuery<InstallationView>;
