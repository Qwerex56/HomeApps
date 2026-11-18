namespace HomeApps.Contracts.Devices;

/// <summary>
/// A household satellite was removed or got a new pairing code: Auth stops its tokens and Households removes it from
/// the members. Pairing again sends <see cref="HouseholdDevicePaired"/> anew.
/// </summary>
public sealed record HouseholdDeviceUnpaired(Guid DeviceId, Guid HouseholdId) : IIntegrationEvent;
