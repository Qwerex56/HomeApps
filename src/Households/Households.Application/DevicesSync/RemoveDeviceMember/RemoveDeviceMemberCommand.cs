using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.DevicesSync.RemoveDeviceMember;

/// <summary>S13, from Devices' <c>HouseholdDeviceUnpaired</c>: the satellite leaves its household.</summary>
public sealed record RemoveDeviceMemberCommand(Guid DeviceId, Guid HouseholdId) : ICommand;
