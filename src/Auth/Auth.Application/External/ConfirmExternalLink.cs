using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.External;

/// <summary>
/// A-15 "yes": the user proves the existing account with its password, then the Google/Apple login is linked and the
/// user is signed in. "No" needs no command: the ticket simply expires and nothing was created (R17).
/// An account without a password cannot confirm here yet; it links from "my sign-in methods" instead (A-11).
/// </summary>
public sealed record ConfirmExternalLinkCommand(string Ticket, string Password, string ClientId, string? UserAgent)
    : ICommand<SignInResult>;

internal sealed class ConfirmExternalLinkValidator : AbstractValidator<ConfirmExternalLinkCommand>
{
    public ConfirmExternalLinkValidator()
    {
        RuleFor(c => c.Ticket).NotEmpty();
        RuleFor(c => c.Password).NotEmpty();
        RuleFor(c => c.ClientId).NotEmpty();
    }
}

internal sealed class ConfirmExternalLinkHandler(
    IIdentityRepository identities,
    IClientRegistry clients,
    ITokenIssuer tokens,
    IPasswordHasher hasher,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ICommandHandler<ConfirmExternalLinkCommand, SignInResult>
{
    public async Task<Result<SignInResult>> Handle(ConfirmExternalLinkCommand command, CancellationToken cancellationToken)
    {
        var client = clients.Resolve(command.ClientId, command.UserAgent);
        if (client.IsFailed)
        {
            return client.ToResult<SignInResult>();
        }

        if (tokens.ReadTicket(command.Ticket) is not ExternalLinkTicket ticket)
        {
            return Result.Fail<SignInResult>(ApplicationErrors.TicketInvalid);
        }

        var identity = await identities.GetByIdAsync(ticket.IdentityId, cancellationToken);
        var email = Email.Create(ticket.Email);
        if (identity is null || email.IsFailed)
        {
            return Result.Fail<SignInResult>(ApplicationErrors.TicketInvalid);
        }

        // Password first: a failed attempt is saved for the lockout (P4) and must not save a half-done link.
        var outcome = identity.SignInWithPassword(command.Password, client.Value.Info, hasher, time);
        if (outcome.IsFailed)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return outcome.ToResult<SignInResult>();
        }

        var loginTaken = await identities.ExternalLoginExistsAsync(ticket.Provider, ticket.Subject, cancellationToken);
        var linked = identity.LinkExternalLogin(ticket.Provider, ticket.Subject, email.Value, loginTaken, time);
        return linked.IsFailed
            ? linked.ToResult<SignInResult>()
            : tokens.ToResult(outcome.Value, identity, client.Value.Registered);
    }
}
