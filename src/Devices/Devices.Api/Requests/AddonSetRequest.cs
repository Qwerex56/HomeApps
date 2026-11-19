using System.Text.Json;

namespace Devices.Api.Requests;

/// <summary>A HomeMix add-on's trigger sets a control (<c>POST /internal/devices/{id}/set</c>); see <c>SetControlForAddonCommand</c>.</summary>
public sealed record AddonSetRequest(
    Guid HouseholdId, string? Control, JsonElement? Value, Guid InstallerUserId, Guid InstallationId, string? AddonName);
