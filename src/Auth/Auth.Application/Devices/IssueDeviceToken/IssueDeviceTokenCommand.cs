using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Devices.IssueDeviceToken;

/// <summary>
/// A paired satellite asks for a 15-minute access token with its secret (like OAuth's client credentials). The token
/// acts for the household: <c>sub</c> is the device, <c>act</c> is <c>device</c>, so Auth, Accounts and Households
/// refuse its changes while plugins treat it as the member it is.
/// </summary>
public sealed record IssueDeviceTokenCommand(Guid DeviceId, string Secret) : ICommand<DeviceTokenView>;
