using Auth.Application.Abstractions;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.MyAccount.SetPin;

internal sealed class SetPinHandler(IIdentityRepository identities, ICurrentUser currentUser, IPasswordHasher hasher, TimeProvider time)
    : ICommandHandler<SetPinCommand>
{
    public async Task<Result> Handle(SetPinCommand command, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        if (identity is null)
        {
            return Result.Fail(IdentityErrors.CannotSignIn);
        }

        return identity.SetPin(command.CurrentPassword, currentUser.AuthenticatedAt ?? DateTimeOffset.MinValue, command.Pin, hasher, time);
    }
}
