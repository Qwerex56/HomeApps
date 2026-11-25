using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin;

internal sealed class DisableUserHandler(IUserRepository users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<DisableUserCommand>
{
    public Task<Result> Handle(DisableUserCommand command, CancellationToken cancellationToken) =>
        users.ForAdminAsync(currentUser, command.UserId, u => u.Disable(currentUser.Actor(), command.Reason, time), cancellationToken);
}
