using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Devices.Application.Common;

namespace Devices.Application.HouseholdDevices.GetHouseholdDevices;

/// <summary>The household's devices for any member, removed ones last; changing them is for admins.</summary>
public sealed record GetHouseholdDevicesQuery(Guid HouseholdId) : IQuery<IReadOnlyList<DeviceView>>;
