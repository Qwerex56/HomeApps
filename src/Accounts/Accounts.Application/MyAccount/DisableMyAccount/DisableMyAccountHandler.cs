using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.MyAccount;

internal sealed class DisableMyAccountHandler(IUserRepository users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<DisableMyAccountCommand>
{
    public async Task<Result> Handle(DisableMyAccountCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        return user is null
            ? Result.Fail(ApplicationErrors.UserPending)
            : user.Disable(currentUser.Actor(), command.Reason, time);
    }
}
