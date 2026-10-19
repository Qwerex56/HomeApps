using Auth.Application.Abstractions;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using Auth.Domain.Invitations;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Invitations;

/// <summary>A-20: any active account invites an email (R10, R14). The link goes out by email; the caller gets the code.</summary>
public sealed record CreateInvitationCommand(string Email) : ICommand<CreatedInvitation>;

public sealed record CreatedInvitation(Guid Id, string Code, DateTimeOffset ExpiresAt);

internal sealed class CreateInvitationValidator : AbstractValidator<CreateInvitationCommand>
{
    public CreateInvitationValidator() => RuleFor(c => c.Email).NotEmpty();
}

internal sealed class CreateInvitationHandler(
    IIdentityRepository identities,
    IInvitationRepository invitations,
    ICurrentUser currentUser,
    TimeProvider time) : ICommandHandler<CreateInvitationCommand, CreatedInvitation>
{
    public async Task<Result<CreatedInvitation>> Handle(CreateInvitationCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        if (email.IsFailed)
        {
            return email.ToResult<CreatedInvitation>();
        }

        var me = await identities.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        if (me is null)
        {
            return Result.Fail<CreatedInvitation>(IdentityErrors.CannotSignIn);
        }

        var now = time.GetUtcNow();
        var issued = Invitation.Create(
            new Inviter(me.Id, me.Email, me.CanSignIn(now)),
            email.Value,
            await identities.EmailExistsAsync(email.Value, cancellationToken),
            await invitations.HasPendingForAsync(email.Value, now, cancellationToken),
            time);
        if (issued.IsFailed)
        {
            return issued.ToResult<CreatedInvitation>();
        }

        var invitation = issued.Value.Invitation;
        invitations.Add(invitation);
        return new CreatedInvitation(invitation.Id, invitation.Code.Value, invitation.ExpiresAt);
    }
}
