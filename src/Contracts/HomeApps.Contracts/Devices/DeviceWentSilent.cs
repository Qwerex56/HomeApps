namespace HomeApps.Contracts.Devices;

/// <summary>
/// A smart_home or assistant device sent no status report for 2.5 of its heartbeat periods: it is "silent" (missing),
/// which is not the same as offline after a disconnect. Published once until the device reports again.
/// </summary>
/// <param name="HouseholdId">The household the device belongs to.</param>
/// <param name="DeviceId">The device.</param>
/// <param name="Name">The device's display name.</param>
/// <param name="LastStatusAt">When its last status report came, or <c>null</c> when it never sent one.</param>
public sealed record DeviceWentSilent(Guid HouseholdId, Guid DeviceId, string Name, DateTimeOffset? LastStatusAt) : IIntegrationEvent;
