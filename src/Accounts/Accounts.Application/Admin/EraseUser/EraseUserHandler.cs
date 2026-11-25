using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin;

internal sealed class EraseUserHandler(IUserRepository users, IAvatarStore avatars, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<EraseUserCommand>
{
    public async Task<Result> Handle(EraseUserCommand command, CancellationToken cancellationToken)
    {
        var result = await users.ForAdminAsync(currentUser, command.UserId, user =>
        {
            var erased = user.Erase(currentUser.Actor(), command.ConfirmEmail, time);
            if (erased.IsFailed)
            {
                return erased.ToResult();
            }

            users.Remove(user);
            users.AddErased(erased.Value);
            return Result.Ok();
        }, cancellationToken);

        // The uploaded picture is personal data too (2.15).
        if (result.IsSuccess)
        {
            await avatars.DeleteAsync(command.UserId, cancellationToken);
        }

        return result;
    }
}
