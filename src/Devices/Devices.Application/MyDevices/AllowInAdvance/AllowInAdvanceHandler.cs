using Devices.Application.Abstractions;
using Devices.Application.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.MyDevices.AllowInAdvance;

internal sealed class AllowInAdvanceHandler(IDeviceRepository devices, ICurrentUser currentUser) : ICommandHandler<AllowInAdvanceCommand>
{
    public async Task<Result> Handle(AllowInAdvanceCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.FindMineAsync(command.DeviceId, currentUser.RequiredUserId, cancellationToken);
        return device.IsFailed ? device.ToResult() : device.Value.AllowInAdvance(command.Action, command.Allowed);
    }
}
