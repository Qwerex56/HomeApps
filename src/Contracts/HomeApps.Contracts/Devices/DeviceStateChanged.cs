namespace HomeApps.Contracts.Devices;

/// <summary>
/// A household device reported state values that differ from the ones Devices stored (household-jarvis-devices.md 2.5,
/// D8), so plugins can react. A frame that repeats what is already known publishes nothing.
/// </summary>
/// <param name="HouseholdId">The household the device belongs to.</param>
/// <param name="DeviceId">The device that reported.</param>
/// <param name="Slug">The device's stable key, e.g. <c>fotel</c>.</param>
/// <param name="Changed">A JSON object with only the values that changed (control and sensor names, namespaced as the device does) and their new values.</param>
/// <param name="At">When Devices applied the frame.</param>
public sealed record DeviceStateChanged(Guid HouseholdId, Guid DeviceId, string Slug, string Changed, DateTimeOffset At) : IIntegrationEvent;
