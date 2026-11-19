using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Devices.Application.Common;

namespace Devices.Application.HouseholdDevices.GetHouseholdDevices;

/// <summary>
/// The household's devices for any member, removed ones last, with each one's purpose, name, slug and descriptor summary. Guests too (D6); notes for JARVIS only to admins.
/// With <paramref name="Capability"/>, <paramref name="Role"/> or <paramref name="Model"/> only matching devices that are not removed
/// (how plugins and add-ons find devices, 2.5); with <paramref name="WithState"/> each carries its last state.
/// </summary>
public sealed record GetHouseholdDevicesQuery(
    Guid HouseholdId, string? Capability = null, string? Role = null, string? Model = null, bool WithState = false)
    : IQuery<IReadOnlyList<DeviceView>>;
