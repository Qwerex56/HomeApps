using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using HomeApps.Contracts.Households;

namespace Devices.Application.HouseholdDevices.AddHouseholdDevice;

internal sealed class AddHouseholdDeviceHandler(
    IDeviceRepository devices, IHouseholdRoles households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<AddHouseholdDeviceCommand, PairingCodeView>
{
    public async Task<Result<PairingCodeView>> Handle(AddHouseholdDeviceCommand command, CancellationToken cancellationToken)
    {
        switch (await households.GetRoleAsync(command.HouseholdId, cancellationToken))
        {
            case null:
                return Result.Fail(DeviceErrors.NotFound);
            case < HouseholdRole.Admin:
                return Result.Fail(DeviceErrors.Forbidden);
        }

        var name = DeviceName.Normalize(command.Name);
        if (name.IsFailed)
        {
            return name.ToResult();
        }

        if (await devices.FindInHouseholdAsync(command.HouseholdId, name.Value, cancellationToken) is not null)
        {
            return Result.Fail(DeviceErrors.NameTaken);
        }

        var code = PairingCode.New();
        var now = time.GetUtcNow();
        var added = Device.AddToHousehold(command.HouseholdId, currentUser.RequiredUserId, name.Value, PairingCode.Hash(code),
            await devices.SlugsAsync(currentUser.RequiredUserId, command.HouseholdId, cancellationToken), now);
        if (added.IsFailed)
        {
            return added.ToResult();
        }

        devices.Add(added.Value);
        return new PairingCodeView(added.Value.Id, added.Value.Name, code, added.Value.PairingExpiresAt!.Value);
    }
}
