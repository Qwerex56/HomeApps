using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Installations.RemoveInstallation;

/// <summary>Uninstalls an add-on; its data stays for 30 days, and a reinstall in that time brings it back (DataRetention).</summary>
public sealed record RemoveInstallationCommand(Guid InstallationId) : ICommand;
