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
/// <param name="ChangedBy">Who asked for the change when it was a set by an add-on in the last few seconds (<c>homemix:{installationId}</c>), so that one does not react to its own change; <c>null</c> for a change the device made itself or someone else asked for.</param>
/// <param name="Device">The device's name, model, roles and capabilities, so a listener can match it; <c>null</c> from a sender that predates it.</param>
public sealed record DeviceStateChanged(
    Guid HouseholdId, Guid DeviceId, string Slug, string Changed, DateTimeOffset At, string? ChangedBy = null, DeviceInfo? Device = null) : IIntegrationEvent;
