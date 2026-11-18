using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Devices;
using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Devices;

namespace Devices.Application.HouseholdDevices.NewPairingCode;

internal sealed class NewPairingCodeHandler(
    IDeviceRepository devices, IHouseholdRoles households, IDeviceLinks links, IEventBus bus, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<NewPairingCodeCommand, PairingCodeView>
{
    public async Task<Result<PairingCodeView>> Handle(NewPairingCodeCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.FindManageableAsync(households, command.DeviceId, currentUser.RequiredUserId, cancellationToken);
        if (device.IsFailed)
        {
            return device.ToResult();
        }

        if (!device.Value.IsHouseholdDevice)
        {
            return Result.Fail(DeviceErrors.NotFound);
        }

        if (device.Value.IsPairedSatellite)
        {
            // S13: the old hardware leaves the household until the new code is used.
            await bus.PublishAsync(new HouseholdDeviceUnpaired(device.Value.Id, device.Value.HouseholdId!.Value), cancellationToken);
        }

        var code = PairingCode.New();
        device.Value.NewPairingCode(PairingCode.Hash(code), time.GetUtcNow());
        links.Drop(device.Value.Id);
        return new PairingCodeView(device.Value.Id, device.Value.Name, code, device.Value.PairingExpiresAt!.Value);
    }
}
