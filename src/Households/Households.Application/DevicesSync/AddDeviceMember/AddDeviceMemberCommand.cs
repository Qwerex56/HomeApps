using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.DevicesSync.AddDeviceMember;

/// <summary>
/// jarvis-satellites.md 3a (S13), from Devices' <c>HouseholdDevicePaired</c>: the satellite becomes a Member of its
/// household, shown as "JARVIS · <paramref name="Name"/>". A second delivery changes nothing.
/// </summary>
public sealed record AddDeviceMemberCommand(Guid DeviceId, Guid HouseholdId, string Name) : ICommand;
