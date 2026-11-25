using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin;

internal sealed class EraseUserHandler(IUserRepository users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<EraseUserCommand>
{
    public Task<Result> Handle(EraseUserCommand command, CancellationToken cancellationToken) =>
        users.ForAdminAsync(currentUser, command.UserId, user =>
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
}
