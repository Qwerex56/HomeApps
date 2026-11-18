using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.HouseholdDevices.SetJarvisAccess;

/// <summary>A household admin (D6): may JARVIS control this device, and does it ask for a "tak" first ("Nie pytaj" is <paramref name="Asks"/> off).</summary>
public sealed record SetJarvisAccessCommand(Guid DeviceId, bool MayControl, bool Asks) : ICommand;
