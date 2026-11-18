using Auth.Application.Abstractions;
using Auth.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Devices;

namespace Auth.Application.Devices.PairDevice;

internal sealed class PairDeviceHandler(IClientRegistry clients, IDeviceIdentityRepository devices, IEventBus bus, TimeProvider time)
    : ICommandHandler<PairDeviceCommand, PairedDeviceView>
{
    /// <summary>Only Devices pairs satellites; other service clients (JARVIS) may not.</summary>
    public const string DevicesClientId = "devices-service";

    public async Task<Result<PairedDeviceView>> Handle(PairDeviceCommand command, CancellationToken cancellationToken)
    {
        if (command.ClientId != DevicesClientId || clients.AuthenticateService(command.ClientId, command.ClientSecret) is null)
        {
            return Result.Fail(DeviceIdentityErrors.ClientInvalid);
        }

        var now = time.GetUtcNow();
        var identity = await devices.GetByIdAsync(command.DeviceId, cancellationToken);
        string secret;
        if (identity is null)
        {
            (identity, secret) = DeviceIdentity.Pair(command.DeviceId, command.HouseholdId, command.Name, now);
            devices.Add(identity);
        }
        else
        {
            secret = identity.PairAgain(command.HouseholdId, command.Name, now);
        }

        await bus.PublishAsync(new HouseholdDevicePaired(identity.Id, identity.HouseholdId, identity.Name), cancellationToken);
        return new PairedDeviceView(identity.Id, secret);
    }
}
