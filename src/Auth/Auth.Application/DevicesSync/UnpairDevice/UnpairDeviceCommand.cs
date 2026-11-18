using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.DevicesSync.UnpairDevice;

/// <summary>Devices: <c>HouseholdDeviceUnpaired</c>. The satellite's secret stops working until it pairs again.</summary>
public sealed record UnpairDeviceCommand(Guid DeviceId) : ICommand;
