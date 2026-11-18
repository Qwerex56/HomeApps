namespace Devices.Application.HouseholdDevices;

/// <summary>What a device keeps after pairing (its purpose as the descriptor spells it, e.g. <c>smart_home</c>): its <see cref="Secret"/> buys it tokens at <c>/auth/devices/token</c>.</summary>
public sealed record PairedDeviceView(Guid DeviceId, Guid HouseholdId, string Name, string Secret, string Slug, string Purpose);
