using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Installations.GetMyInstallations;

/// <summary>The add-ons on the caller's own account.</summary>
public sealed record GetMyInstallationsQuery : IQuery<IReadOnlyList<InstallationView>>;
