using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Installations.ResetWidget;

/// <summary>Back to the default in a place: every widget that lists it, in its own size.</summary>
public sealed record ResetWidgetCommand(Guid InstallationId, string Place) : ICommand<InstallationView>;
