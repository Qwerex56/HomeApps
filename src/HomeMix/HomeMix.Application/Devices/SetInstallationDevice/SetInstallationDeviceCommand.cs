using System.Text.Json;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Devices.SetInstallationDevice;

/// <summary>
/// <c>devices.set(id, control, value)</c> from an add-on (capability <c>devices.control</c>): sets a control of a device
/// of the installation's household with the caller's rights; Devices refuses guests.
/// </summary>
public sealed record SetInstallationDeviceCommand(Guid InstallationId, Guid DeviceId, string Control, JsonElement Value) : ICommand;
