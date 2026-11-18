using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.MyDevices.RemoveDevice;

/// <summary>Removes my device: it is disconnected and may not connect again until restored. Its data stays.</summary>
public sealed record RemoveDeviceCommand(Guid DeviceId) : ICommand;
