using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
namespace Devices.Application.HouseholdDevices.NewPairingCode;

/// <summary>A household admin gets a new code for a satellite, e.g. new hardware or an expired code; the old pairing ends.</summary>
public sealed record NewPairingCodeCommand(Guid DeviceId) : ICommand<PairingCodeView>;
