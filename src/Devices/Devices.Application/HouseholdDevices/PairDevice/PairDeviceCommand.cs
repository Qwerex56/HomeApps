using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.HouseholdDevices.PairDevice;

/// <summary>
/// jarvis-satellites.md 3a (S13): a satellite with no account types in the code an admin got for it (anonymous, rate
/// limited). The code is spent, Auth gives the satellite its identity and Households makes it a Member. An unknown,
/// used or expired code gets the same answer. The device may send its descriptor (<c>homeapps.device/1</c>): it sets the
/// device's purpose and capabilities; a satellite that sends none stays a plain assistant.
/// </summary>
public sealed record PairDeviceCommand(string Code, string? Descriptor = null) : ICommand<PairedDeviceView>;
