using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using Auth.Domain.Invitations;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Invitations;

/// <summary>A-07 (a), A-30: sign-up with a password through an invitation link; the email is the invited one.</summary>
public sealed record AcceptInvitationWithPasswordCommand(
    string Token,
    string Password,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? Locale,
    string ClientId,
    string? UserAgent) : ICommand<AuthTokens>;

internal sealed class AcceptInvitationWithPasswordValidator : AbstractValidator<AcceptInvitationWithPasswordCommand>
{
    public AcceptInvitationWithPasswordValidator()
    {
        RuleFor(c => c.Token).NotEmpty();
        RuleFor(c => c.Password).NotEmpty();
        RuleFor(c => c.DisplayName).NotEmpty();
        RuleFor(c => c.ClientId).NotEmpty();
    }
}

internal sealed class AcceptInvitationWithPasswordHandler(
    IIdentityRepository identities,
    IInvitationRepository invitations,
    IClientRegistry clients,
    ITokenIssuer tokens,
    IPasswordHasher hasher,
    TimeProvider time) : ICommandHandler<AcceptInvitationWithPasswordCommand, AuthTokens>
{
    public async Task<Result<AuthTokens>> Handle(AcceptInvitationWithPasswordCommand command, CancellationToken cancellationToken)
    {
        var client = clients.Resolve(command.ClientId, command.UserAgent);
        if (client.IsFailed)
        {
            return client.ToResult<AuthTokens>();
        }

        var invitation = await invitations.GetByTokenHashAsync(OpaqueToken.HashOf(command.Token), cancellationToken);
        if (invitation?.Email is null)
        {
            return Result.Fail<AuthTokens>(InvitationErrors.InvalidOrExpired);
        }

        var profile = RegistrationProfile.Create(command.DisplayName, command.FirstName, command.LastName, null);
        if (profile.IsFailed)
        {
            return profile.ToResult<AuthTokens>();
        }

        var registration = Identity.AcceptInvitationWithPassword(
            invitation,
            command.Password,
            profile.Value,
            new RegistrationContext(command.ClientId, client.Value.Info.UserAgent, command.Locale),
            client.Value.Info,
            await identities.EmailExistsAsync(invitation.Email, cancellationToken),
            hasher,
            time);
        if (registration.IsFailed)
        {
            return registration.ToResult<AuthTokens>();
        }

        identities.Add(registration.Value.Identity);
        return tokens.Tokens(registration.Value.Identity, registration.Value.RefreshToken, client.Value.Registered);
    }
}
