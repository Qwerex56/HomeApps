using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.MyDevices.RestoreDevice;

/// <summary>Adds a removed device of mine back; it may connect again.</summary>
public sealed record RestoreDeviceCommand(Guid DeviceId) : ICommand;
