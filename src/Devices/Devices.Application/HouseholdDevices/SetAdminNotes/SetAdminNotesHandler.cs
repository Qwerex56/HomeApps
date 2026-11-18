using Devices.Application.Abstractions;
using Devices.Application.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.HouseholdDevices.SetAdminNotes;

internal sealed class SetAdminNotesHandler(IDeviceRepository devices, IHouseholdRoles households, ICurrentUser currentUser)
    : ICommandHandler<SetAdminNotesCommand>
{
    public async Task<Result> Handle(SetAdminNotesCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.FindManageableAsync(households, command.DeviceId, currentUser.RequiredUserId, cancellationToken);
        return device.IsFailed ? device.ToResult() : device.Value.SetAdminNotes(command.Notes);
    }
}
