using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Heartbeat.CheckHeartbeats;

/// <summary>
/// The periodic scan for devices that stopped reporting. <paramref name="NotBefore"/> is when the scanning started: time
/// before it does not count, so a server that was down does not find every device silent when it comes back.
/// </summary>
public sealed record CheckHeartbeatsCommand(DateTimeOffset NotBefore) : ICommand<int>;
