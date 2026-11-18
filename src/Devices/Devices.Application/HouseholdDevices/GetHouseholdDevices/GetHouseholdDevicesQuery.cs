using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Devices.Application.Common;

namespace Devices.Application.HouseholdDevices.GetHouseholdDevices;

/// <summary>The household's devices for any member, removed ones last, with each one's purpose, name, slug and descriptor summary. Guests too (D6); notes for JARVIS only to admins.</summary>
public sealed record GetHouseholdDevicesQuery(Guid HouseholdId) : IQuery<IReadOnlyList<DeviceView>>;
