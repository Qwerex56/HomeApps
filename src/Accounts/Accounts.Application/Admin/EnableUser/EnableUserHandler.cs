using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin;

internal sealed class EnableUserHandler(IUserRepository users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<EnableUserCommand>
{
    public Task<Result> Handle(EnableUserCommand command, CancellationToken cancellationToken) =>
        users.ForAdminAsync(currentUser, command.UserId, u => u.Enable(currentUser.Actor(), time), cancellationToken);
}
