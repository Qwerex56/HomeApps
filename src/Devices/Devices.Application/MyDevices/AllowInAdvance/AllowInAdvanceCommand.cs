using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.MyDevices.AllowInAdvance;

/// <summary>"Nie pytaj" (S6) on or off for one action of my device: with it on, JARVIS runs the action without "tak/nie".</summary>
public sealed record AllowInAdvanceCommand(Guid DeviceId, string Action, bool Allowed) : ICommand;
