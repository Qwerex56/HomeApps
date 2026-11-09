using HomeApps.BuildingBlocks.Application.Messaging;

namespace Notifications.Application.Devices.RegisterDevice;

/// <summary>
/// "Turn notifications on" on this device: the browser's push subscription for the signed-in person. Returns the
/// device id. The same endpoint under another person (shared browser) moves to this one.
/// </summary>
public sealed record RegisterDeviceCommand(string Endpoint, string P256dh, string Auth, string? UserAgent) : ICommand<Guid>;
