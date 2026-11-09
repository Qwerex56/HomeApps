using HomeApps.BuildingBlocks.Application.Messaging;

namespace Notifications.Application.Devices.RemoveDevice;

/// <summary>Removes one of my devices from the settings list; it gets nothing more.</summary>
public sealed record RemoveDeviceCommand(Guid DeviceId) : ICommand;
