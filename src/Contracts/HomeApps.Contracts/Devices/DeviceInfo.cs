namespace HomeApps.Contracts.Devices;

/// <summary>
/// What a consumer of <see cref="DeviceStateChanged"/> or <see cref="DeviceEventOccurred"/> needs to match the device
/// (household-jarvis-devices.md 2.5) without asking Devices again: its name and what it says about itself.
/// </summary>
/// <param name="Name">The device's name as the household sees it.</param>
/// <param name="Model">The descriptor's model, e.g. <c>qwerex56/fotel-v1</c>, or <c>null</c>.</param>
/// <param name="Roles">The device's roles, e.g. <c>weather_station</c>.</param>
/// <param name="Capabilities">As Devices lists them: <c>weather</c>, or <c>switch:lampka</c> for an instance.</param>
public sealed record DeviceInfo(string Name, string? Model, IReadOnlyList<string> Roles, IReadOnlyList<string> Capabilities);
