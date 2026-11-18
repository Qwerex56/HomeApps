using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
namespace Devices.Application.HouseholdDevices;

/// <summary>Shown once to the admin: the device pairs with <paramref name="Code"/> before <paramref name="ExpiresAt"/>.</summary>
public sealed record PairingCodeView(Guid DeviceId, string Name, string Code, DateTimeOffset ExpiresAt);
