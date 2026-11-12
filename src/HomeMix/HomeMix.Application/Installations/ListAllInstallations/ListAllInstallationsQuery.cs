using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Installations.ListAllInstallations;

/// <summary>Every active installation on the instance, newest first, for the Owner's "Dodatki" view (A5).</summary>
public sealed record ListAllInstallationsQuery : IQuery<IReadOnlyList<AdminInstallationView>>;
