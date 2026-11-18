using Households.Application.Abstractions;
using Households.Domain.Users;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.DevicesSync.AddDeviceMember;

internal sealed class AddDeviceMemberHandler(IHouseholdRepository households, IUserCopies users, TimeProvider time)
    : ICommandHandler<AddDeviceMemberCommand>
{
    private const string NamePrefix = "JARVIS · ";

    public async Task<Result> Handle(AddDeviceMemberCommand command, CancellationToken cancellationToken)
    {
        var household = await households.GetByIdAsync(command.HouseholdId, cancellationToken);
        if (household is null)
        {
            // Gone since the code was made: nothing to join.
            return Result.Ok();
        }

        var name = NamePrefix + command.Name;
        if (await users.GetAsync(command.DeviceId, cancellationToken) is { } copy)
        {
            copy.UpdateProfile(name, null);
        }
        else
        {
            users.Add(UserCopy.Create(command.DeviceId, name, null));
        }

        // An archived household refuses it; retrying would not change that.
        _ = household.AddDevice(command.DeviceId, time);
        return Result.Ok();
    }
}
