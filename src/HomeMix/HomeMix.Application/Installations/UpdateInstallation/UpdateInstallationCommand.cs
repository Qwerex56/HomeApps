using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Installations.UpdateInstallation;

/// <summary>Moves an installation to another package of the same add-on; <paramref name="Consent"/> only when it asks for more.</summary>
public sealed record UpdateInstallationCommand(Guid InstallationId, string Sha256, ConsentInput? Consent) : ICommand<InstallationView>;
