using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.HouseholdDevices.RenameDevice;

/// <summary>
/// A household admin (D6), or the person for their own computer, changes a device's display name: any text of up to 60
/// characters, unique in the household regardless of case. The slug, and so the tool names, stay.
/// </summary>
public sealed record RenameDeviceCommand(Guid DeviceId, string Name) : ICommand;
