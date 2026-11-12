using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Installations.RemoveInstallation;

/// <summary>Uninstalls an add-on; its data stays for 30 days (A2).</summary>
public sealed record RemoveInstallationCommand(Guid InstallationId) : ICommand;
