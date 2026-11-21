using Devices.Application.Abstractions;
using Devices.Application.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.HouseholdDevices.SetAdminNotes;

internal sealed class SetAdminNotesHandler(IDeviceRepository devices, IHouseholdRoles households, IDeviceStream stream, ICurrentUser currentUser)
    : ICommandHandler<SetAdminNotesCommand>
{
    public async Task<Result> Handle(SetAdminNotesCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.FindManageableAsync(households, command.DeviceId, currentUser.RequiredUserId, cancellationToken);
        if (device.IsFailed)
        {
            return device.ToResult();
        }

        var set = device.Value.SetAdminNotes(command.Notes);
        if (set.IsSuccess && device.Value.HouseholdId is { } householdId)
        {
            stream.Publish(new DeviceStreamMessage(householdId, "changed", new { deviceId = device.Value.Id, reason = "notes" }));
        }

        return set;
    }
}
