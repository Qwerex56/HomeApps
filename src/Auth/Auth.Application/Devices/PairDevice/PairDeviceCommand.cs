using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Devices.PairDevice;

/// <summary>
/// jarvis-satellites.md 3a (S13): Devices checked the household admin's one-time code and asks, on the internal
/// network and with its service secret, for the satellite's own identity. Pairing again (new hardware) replaces the
/// secret. Households then makes the device a member (<c>HouseholdDevicePaired</c>).
/// </summary>
public sealed record PairDeviceCommand(string ClientId, string ClientSecret, Guid DeviceId, Guid HouseholdId, string Name)
    : ICommand<PairedDeviceView>;
