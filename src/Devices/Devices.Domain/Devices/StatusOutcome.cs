namespace Devices.Domain.Devices;

/// <summary>What a status report changed: the device <paramref name="Returned"/> from silence, or its battery <paramref name="BatteryLow"/> just fell below the threshold.</summary>
public readonly record struct StatusOutcome(bool Returned, bool BatteryLow);
