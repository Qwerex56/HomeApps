using System.Text.Json;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Devices.GetInstallationDeviceState;

/// <summary><c>devices.state(id)</c> from an add-on (capability <c>devices</c>): the last state of a device of the installation's household, as Devices answers it.</summary>
public sealed record GetInstallationDeviceStateQuery(Guid InstallationId, Guid DeviceId) : IQuery<JsonElement>;
