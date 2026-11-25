using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.MyAccount;

internal sealed class UpdateMyProfileHandler(IUserRepository users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<UpdateMyProfileCommand>
{
    public async Task<Result> Handle(UpdateMyProfileCommand command, CancellationToken cancellationToken)
    {
        var profile = command.Profile.ToProfile(time);
        if (profile.IsFailed)
        {
            return profile.ToResult();
        }

        var user = await users.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        return user is null
            ? Result.Fail(ApplicationErrors.UserPending)
            : user.UpdateProfile(currentUser.Actor(), profile.Value, time);
    }
}
