using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Devices.ChooseWidgetDevice;

/// <summary>
/// The household's admins choose which device a widget shows (the manual pick of household-jarvis-devices.md 2.5), from the
/// devices its manifest matches; <c>null</c> goes back to the best match.
/// </summary>
public sealed record ChooseWidgetDeviceCommand(Guid InstallationId, string WidgetId, Guid? DeviceId) : ICommand<Installations.InstallationView>;
