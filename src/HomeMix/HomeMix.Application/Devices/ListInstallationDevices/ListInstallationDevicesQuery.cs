using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Devices.ListInstallationDevices;

/// <summary>
/// <c>devices.list()</c> from an add-on (capability <c>devices</c>): the installation's household devices. With
/// <paramref name="WidgetId"/> the ones that widget's manifest matches, best first, with the one to show; without it
/// every device, narrowed by the optional filters.
/// </summary>
public sealed record ListInstallationDevicesQuery(
    Guid InstallationId, string? WidgetId, string? Capability, string? Role, string? Model, bool WithState)
    : IQuery<InstallationDevicesView>;
