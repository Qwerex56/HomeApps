using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using Auth.Domain.Invitations;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Options;

namespace Auth.Application.External;

/// <summary>
/// A-05, A-06, A-07 (b): the callback from Google or Apple. A known login signs in; an email that already has an
/// account asks for consent to link (A-15); anything else is a sign-up, through <paramref name="InvitationToken"/>
/// when the flow started from an invitation link.
/// </summary>
public sealed record SignInExternalCommand(
    ExternalProvider Provider,
    string Code,
    string CodeVerifier,
    Uri RedirectUri,
    string? InvitationToken,
    string? Locale,
    string ClientId,
    string? UserAgent) : ICommand<SignInResult>;

internal sealed class SignInExternalValidator : AbstractValidator<SignInExternalCommand>
{
    public SignInExternalValidator()
    {
        RuleFor(c => c.Provider).IsInEnum();
        RuleFor(c => c.Code).NotEmpty();
        RuleFor(c => c.CodeVerifier).NotEmpty();
        RuleFor(c => c.RedirectUri).NotNull();
        RuleFor(c => c.ClientId).NotEmpty();
    }
}

internal sealed class SignInExternalHandler(
    IIdentityRepository identities,
    IInvitationRepository invitations,
    IExternalIdentityVerifier verifier,
    IClientRegistry clients,
    ITokenIssuer tokens,
    IOptions<AuthOptions> options,
    TimeProvider time) : ICommandHandler<SignInExternalCommand, SignInResult>
{
    public async Task<Result<SignInResult>> Handle(SignInExternalCommand command, CancellationToken cancellationToken)
    {
        var client = clients.Resolve(command.ClientId, command.UserAgent);
        if (client.IsFailed)
        {
            return client.ToResult<SignInResult>();
        }

        var external = await verifier.VerifyAsync(
            command.Provider, command.Code, command.CodeVerifier, command.RedirectUri, cancellationToken);
        if (external.IsFailed)
        {
            return external.ToResult<SignInResult>();
        }

        var person = external.Value;
        var known = await identities.GetByExternalLoginAsync(command.Provider, person.Subject, cancellationToken);
        if (known is not null)
        {
            var outcome = known.SignInExternal(command.Provider, person.Subject, client.Value.Info, time);
            return outcome.IsFailed
                ? outcome.ToResult<SignInResult>()
                : tokens.ToResult(outcome.Value, known, client.Value.Registered);
        }

        // An unverified address could belong to someone else: it may neither create an account nor link to one.
        if (!person.EmailVerified)
        {
            return Result.Fail<SignInResult>(ApplicationErrors.ExternalEmailNotVerified);
        }

        var email = Email.Create(person.Email);
        if (email.IsFailed)
        {
            return email.ToResult<SignInResult>();
        }

        var owner = await identities.GetByEmailAsync(email.Value, cancellationToken);
        if (owner is not null)
        {
            var ticket = tokens.IssueTicket(new ExternalLinkTicket(owner.Id, command.Provider, person.Subject, email.Value.Value));
            return new SignInResult.LinkRequired(ticket, command.Provider, email.Value.Value);
        }

        return await Register(command, person, email.Value, client.Value, cancellationToken);
    }

    private async Task<Result<SignInResult>> Register(
        SignInExternalCommand command, ExternalIdentity person, Email email, ResolvedClient client, CancellationToken cancellationToken)
    {
        Invitation? invitation = null;
        if (command.InvitationToken is not null)
        {
            invitation = await invitations.GetByTokenHashAsync(OpaqueToken.HashOf(command.InvitationToken), cancellationToken);
            if (invitation is null)
            {
                return Result.Fail<SignInResult>(InvitationErrors.InvalidOrExpired);
            }
        }

        var profile = RegistrationProfile.Create(DisplayName(person, email), person.FirstName, person.LastName, person.AvatarUrl);
        if (profile.IsFailed)
        {
            return profile.ToResult<SignInResult>();
        }

        // Both "taken" checks already passed above: no identity has this login or this email.
        var registration = Identity.RegisterExternal(
            command.Provider,
            person.Subject,
            email,
            profile.Value,
            new RegistrationContext(command.ClientId, client.Info.UserAgent, command.Locale),
            client.Info,
            invitation,
            emailTaken: false,
            loginTaken: false,
            options.Value.RegistrationOpen,
            time);
        if (registration.IsFailed)
        {
            return registration.ToResult<SignInResult>();
        }

        identities.Add(registration.Value.Identity);
        return new SignInResult.SignedIn(
            tokens.Tokens(registration.Value.Identity, registration.Value.RefreshToken, client.Registered));
    }

    /// <summary>The provider's name, or the part of the email before the @ when it gives none.</summary>
    private static string DisplayName(ExternalIdentity person, Email email)
    {
        var name = string.Join(' ', new[] { person.FirstName, person.LastName }.Where(n => !string.IsNullOrWhiteSpace(n)));
        if (name.Length == 0)
        {
            name = email.Value[..email.Value.IndexOf('@', StringComparison.Ordinal)];
        }

        return name.Length > RegistrationProfile.DisplayNameMaxLength ? name[..RegistrationProfile.DisplayNameMaxLength] : name;
    }
}
