using Auth.Application.Abstractions;
using Auth.Domain.Identities;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.MyAccount.ClearPin;

internal sealed class ClearPinHandler(IIdentityRepository identities, ICurrentUser currentUser, TimeProvider time) : ICommandHandler<ClearPinCommand>
{
    public async Task<Result> Handle(ClearPinCommand command, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        if (identity is null)
        {
            return Result.Fail(IdentityErrors.CannotSignIn);
        }

        identity.ClearPin(time);
        return Result.Ok();
    }
}
