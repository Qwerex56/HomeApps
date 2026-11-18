using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
namespace Devices.Application.HouseholdDevices.AddHouseholdDevice;

/// <summary>
/// jarvis-satellites.md 3a (S-1a): a household admin adds a satellite, e.g. <c>salon</c>, and gets a one-time code
/// (10 minutes) to pair it with. Names are unique in the household; a removed device keeps its name until restored.
/// </summary>
public sealed record AddHouseholdDeviceCommand(Guid HouseholdId, string Name) : ICommand<PairingCodeView>;
