using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Installations.Install;

/// <summary>
/// Installs a package added before (by its SHA-256) in a household (<paramref name="HouseholdId"/>) or, without one, on
/// the caller's account, with what the installer accepted.
/// </summary>
public sealed record InstallCommand(string Sha256, TargetKind Target, Guid? HouseholdId, ConsentInput Consent) : ICommand<InstallationView>;
