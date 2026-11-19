using System.Text.Json;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Controls.SetControlForAddon;

/// <summary>
/// A HomeMix add-on trigger changes a control (D8, service to service: there is no person's token). It acts as the add-on
/// within its consent and the rights of <paramref name="InstallerUserId"/>, who must be Admin or Owner of the household now.
/// </summary>
/// <param name="HouseholdId">The add-on's household; the device must belong to it.</param>
/// <param name="InstallationId">The add-on's installation; its source <c>homemix:{id}</c> is carried to the next <c>DeviceStateChanged</c>.</param>
/// <param name="AddonName">The add-on's name, for the device's history ("ustawione przez dodatek X").</param>
public sealed record SetControlForAddonCommand(
    Guid DeviceId, Guid HouseholdId, string? Control, JsonElement? Value, Guid InstallerUserId, Guid InstallationId, string? AddonName) : ICommand;
