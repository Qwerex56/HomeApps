namespace Devices.Application.HouseholdDevices;

/// <summary>What a satellite keeps after pairing: its <see cref="Secret"/> buys it tokens at <c>/auth/devices/token</c>.</summary>
public sealed record PairedDeviceView(Guid DeviceId, Guid HouseholdId, string Name, string Secret);
