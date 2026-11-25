using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin;

internal sealed class UpdateUserProfileHandler(IUserRepository users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<UpdateUserProfileCommand>
{
    public async Task<Result> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken)
    {
        var profile = command.Profile.ToProfile(time);
        if (profile.IsFailed)
        {
            return profile.ToResult();
        }

        return await users.ForAdminAsync(currentUser, command.UserId, u => u.UpdateProfile(currentUser.Actor(), profile.Value, time), cancellationToken);
    }
}
