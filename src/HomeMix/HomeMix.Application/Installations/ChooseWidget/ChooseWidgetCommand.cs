using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Installations.ChooseWidget;

/// <summary>Which widget stands in a place (<c>Household</c>, <c>Wall</c>, <c>Account</c>), its size and order; no widget turns them off.</summary>
public sealed record ChooseWidgetCommand(Guid InstallationId, string Place, string? WidgetId, string? Size, int Order)
    : ICommand<InstallationView>;
