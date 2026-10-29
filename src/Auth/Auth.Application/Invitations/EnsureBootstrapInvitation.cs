using Auth.Application.Abstractions;
using Auth.Domain.Common;
using Auth.Domain.Invitations;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Options;

namespace Auth.Application.Invitations;

/// <summary>
/// A-30, sent once at service start: while there is no Owner and no pending bootstrap invitation, invite
/// <c>Auth:Bootstrap:Email</c>. The link reaches the logs (and the mailbox, with SMTP) through the email message.
/// </summary>
public sealed record EnsureBootstrapInvitationCommand : ICommand;

internal sealed class EnsureBootstrapInvitationHandler(
    IIdentityRepository identities,
    IInvitationRepository invitations,
    IOptions<AuthOptions> options,
    TimeProvider time) : ICommandHandler<EnsureBootstrapInvitationCommand>
{
    public async Task<Result> Handle(EnsureBootstrapInvitationCommand command, CancellationToken cancellationToken)
    {
        var configured = options.Value.Bootstrap.Email;
        if (string.IsNullOrWhiteSpace(configured)
            || await identities.OwnerExistsAsync(cancellationToken)
            || await invitations.HasPendingBootstrapAsync(time.GetUtcNow(), cancellationToken))
        {
            return Result.Ok();
        }

        var email = Email.Create(configured);
        if (email.IsFailed)
        {
            return email.ToResult();
        }

        invitations.Add(Invitation.CreateBootstrap(email.Value, time).Invitation);
        return Result.Ok();
    }
}
