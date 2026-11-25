using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Admin;

internal sealed class ChangeRoleHandler(IIdentityRepository identities, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<ChangeRoleCommand>
{
    public async Task<Result> Handle(ChangeRoleCommand command, CancellationToken cancellationToken)
    {
        var target = await identities.GetByIdAsync(command.IdentityId, cancellationToken);
        return target is null
            ? Result.Fail(ApplicationErrors.IdentityNotFound)
            : target.ChangeRole(currentUser.RequiredUserId, currentUser.ServerRole(), command.Role, time);
    }
}
