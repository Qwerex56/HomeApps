using Households.Application.Abstractions;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.DevicesSync.RemoveDeviceMember;

internal sealed class RemoveDeviceMemberHandler(IHouseholdRepository households, TimeProvider time)
    : ICommandHandler<RemoveDeviceMemberCommand>
{
    public async Task<Result> Handle(RemoveDeviceMemberCommand command, CancellationToken cancellationToken)
    {
        (await households.GetByIdAsync(command.HouseholdId, cancellationToken))?.RemoveDevice(command.DeviceId, time);
        return Result.Ok();
    }
}
