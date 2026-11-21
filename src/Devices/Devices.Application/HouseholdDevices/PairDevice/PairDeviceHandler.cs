using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.HouseholdDevices.PairDevice;

internal sealed class PairDeviceHandler(
    IDeviceRepository devices, ISatelliteIdentities identities, DescriptorIntake intake, IDeviceStream stream, TimeProvider time)
    : ICommandHandler<PairDeviceCommand, PairedDeviceView>
{
    public async Task<Result<PairedDeviceView>> Handle(PairDeviceCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.FindByPairingCodeAsync(PairingCode.Hash(command.Code), cancellationToken);
        if (device is null)
        {
            return Result.Fail(DeviceErrors.PairingExpired);
        }

        var paired = device.Pair(time.GetUtcNow());
        if (paired.IsFailed)
        {
            return paired;
        }

        if (command.Descriptor is not null)
        {
            var described = await intake.ApplyAsync(device, command.Descriptor, cancellationToken);
            if (described.IsFailed)
            {
                return described.ToResult();
            }
        }

        // Nothing is saved unless Auth answered, so a failed call leaves the code usable.
        var secret = await identities.PairAsync(device.Id, device.HouseholdId!.Value, device.Name, cancellationToken);
        if (secret.IsFailed)
        {
            return secret.ToResult();
        }

        stream.Publish(new DeviceStreamMessage(device.HouseholdId.Value, "changed", new { deviceId = device.Id, reason = "paired" }));
        return new PairedDeviceView(device.Id, device.HouseholdId.Value, device.Name, secret.Value, device.Slug, device.Purpose.ToWire());
    }
}
