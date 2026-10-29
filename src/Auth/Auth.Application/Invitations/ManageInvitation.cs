using Auth.Application.Abstractions;
using Auth.Application.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Invitations;

/// <summary>A-21: a new link and 7 more days for a pending or expired invitation (Q2). The inviter, Admin or Owner.</summary>
public sealed record ResendInvitationCommand(Guid InvitationId) : ICommand;

internal sealed class ResendInvitationHandler(IInvitationRepository invitations, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<ResendInvitationCommand>
{
    public async Task<Result> Handle(ResendInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await invitations.GetByIdAsync(command.InvitationId, cancellationToken);
        return invitation is null
            ? Result.Fail(ApplicationErrors.InvitationNotFound)
            : invitation.Resend(currentUser.RequiredUserId, currentUser.ServerRole(), time).ToResult();
    }
}

/// <summary>A-21: the link of a pending invitation stops working.</summary>
public sealed record RevokeInvitationCommand(Guid InvitationId) : ICommand;

internal sealed class RevokeInvitationHandler(IInvitationRepository invitations, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<RevokeInvitationCommand>
{
    public async Task<Result> Handle(RevokeInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await invitations.GetByIdAsync(command.InvitationId, cancellationToken);
        return invitation is null
            ? Result.Fail(ApplicationErrors.InvitationNotFound)
            : invitation.Revoke(currentUser.RequiredUserId, currentUser.ServerRole(), time);
    }
}
