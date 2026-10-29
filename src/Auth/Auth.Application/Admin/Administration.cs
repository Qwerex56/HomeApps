using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Admin;

/// <summary>A-22: Admin or Owner sends a reset link to someone with a lower role and ends their sessions (R10).</summary>
public sealed record ResetPasswordForCommand(Guid IdentityId) : ICommand;

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

/// <summary>A-23: the Owner grants or removes Admin (R18).</summary>
public sealed record ChangeRoleCommand(Guid IdentityId, ServerRole Role) : ICommand;

internal sealed class ChangeRoleValidator : AbstractValidator<ChangeRoleCommand>
{
    public ChangeRoleValidator() => RuleFor(c => c.Role).IsInEnum();
}

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
