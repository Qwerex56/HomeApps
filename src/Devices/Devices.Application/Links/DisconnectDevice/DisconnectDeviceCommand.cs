using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Links.DisconnectDevice;

/// <summary>The device's connection ended: it was last seen now.</summary>
public sealed record DisconnectDeviceCommand(Guid DeviceId) : ICommand;
