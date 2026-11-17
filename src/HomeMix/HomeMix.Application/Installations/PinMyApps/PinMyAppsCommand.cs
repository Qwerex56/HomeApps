using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Installations.PinMyApps;

/// <summary>The add-ons of my account on my quick links bar, in this order; every other one is taken off (UI 2.4.1).</summary>
public sealed record PinMyAppsCommand(IReadOnlyList<Guid> InstallationIds) : ICommand<IReadOnlyList<InstallationView>>;
