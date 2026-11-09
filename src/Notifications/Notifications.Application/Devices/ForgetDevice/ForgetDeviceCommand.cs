using HomeApps.BuildingBlocks.Application.Messaging;

namespace Notifications.Application.Devices.ForgetDevice;

/// <summary>"Turn notifications off" on this device: the browser only knows its endpoint. Unknown endpoints are fine.</summary>
public sealed record ForgetDeviceCommand(string Endpoint) : ICommand;
