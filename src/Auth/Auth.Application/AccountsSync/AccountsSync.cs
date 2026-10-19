using Auth.Application.Abstractions;
using Auth.Domain.Identities;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.AccountsSync;

/// <summary>
/// A-32: <c>UserDisabled</c> / <c>UserEnabled</c> from Accounts, sent by the MassTransit consumers. Idempotent;
/// an unknown user changes nothing.
/// </summary>
public sealed record ApplyAccountStatusCommand(Guid UserId, AccountStatus Status) : ICommand;

internal sealed class ApplyAccountStatusHandler(IIdentityRepository identities, TimeProvider time)
    : ICommandHandler<ApplyAccountStatusCommand>
{
    public async Task<Result> Handle(ApplyAccountStatusCommand command, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByIdAsync(command.UserId, cancellationToken);
        identity?.ApplyStatus(command.Status, time);
        return Result.Ok();
    }
}

/// <summary>
/// A-32, P7: <c>UserErased</c>. The identity goes with its logins and sessions. Invitations this person sent stay as
/// they are; the one they signed up through forgets the address it was sent to.
/// </summary>
public sealed record EraseIdentityCommand(Guid UserId) : ICommand;

internal sealed class EraseIdentityHandler(IIdentityRepository identities, IInvitationRepository invitations)
    : ICommandHandler<EraseIdentityCommand>
{
    public async Task<Result> Handle(EraseIdentityCommand command, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByIdAsync(command.UserId, cancellationToken);
        if (identity is null)
        {
            return Result.Ok();
        }

        var invitation = await invitations.GetAcceptedByAsync(identity.Id, cancellationToken);
        invitation?.ForgetInvitee();
        identities.Remove(identity);
        return Result.Ok();
    }
}
