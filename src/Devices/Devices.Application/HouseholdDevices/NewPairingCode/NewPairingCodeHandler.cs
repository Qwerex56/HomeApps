using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Devices;

namespace Devices.Application.HouseholdDevices.NewPairingCode;

internal sealed class NewPairingCodeHandler(
    IDeviceRepository devices, IHouseholdRoles households, IDeviceLinks links, ICurrentUser currentUser, TimeProvider time)
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

        var code = PairingCode.New();
        device.Value.NewPairingCode(PairingCode.Hash(code), time.GetUtcNow());
        links.Drop(device.Value.Id);
        return new PairingCodeView(device.Value.Id, device.Value.Name, code, device.Value.PairingExpiresAt!.Value);
    }
}
