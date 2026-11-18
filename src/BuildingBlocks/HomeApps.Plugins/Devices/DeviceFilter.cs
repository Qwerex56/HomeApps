namespace HomeApps.Plugins.Devices;

/// <summary>
/// Which devices a plugin wants (household-jarvis-devices.md 2.5): by what they can do (<paramref name="Capability"/>,
/// <c>weather</c>), by <paramref name="Role"/> (<c>weather_station</c>), by <paramref name="Model"/>. Every part given must match.
/// </summary>
public sealed record DeviceFilter(string? Capability = null, string? Role = null, string? Model = null);
