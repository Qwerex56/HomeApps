using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Admin;

internal sealed class ResetPasswordForHandler(IIdentityRepository identities, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<ResetPasswordForCommand>
{
    public async Task<Result> Handle(ResetPasswordForCommand command, CancellationToken cancellationToken)
    {
        var target = await identities.GetByIdAsync(command.IdentityId, cancellationToken);
        return target is null
            ? Result.Fail(ApplicationErrors.IdentityNotFound)
            : target.RequestPasswordResetBy(currentUser.ServerRole(), time);
    }
}
