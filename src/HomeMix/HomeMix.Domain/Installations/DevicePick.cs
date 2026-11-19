namespace HomeMix.Domain.Installations;

/// <summary>The device an admin chose for a widget whose manifest asks to pick by hand, or among several matches (household-jarvis-devices.md 2.5).</summary>
public sealed record DevicePick(string WidgetId, Guid DeviceId);
