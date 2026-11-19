using HomeMix.Application.Abstractions;

namespace HomeMix.Application.Devices;

/// <param name="Devices">The devices, best match first.</param>
/// <param name="Selected">The device a widget shows (its saved pick, else the best automatic match), or <c>null</c>.</param>
/// <param name="NeedsPick">Only manual entries matched and nobody has chosen yet: an admin picks from <paramref name="Devices"/>.</param>
public sealed record InstallationDevicesView(IReadOnlyList<HouseholdDevice> Devices, Guid? Selected, bool NeedsPick);
