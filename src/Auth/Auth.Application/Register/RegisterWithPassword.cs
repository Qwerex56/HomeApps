using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Options;

namespace Auth.Application.Register;

/// <summary>A-01: sign-up with a password, without an invitation. Signs the new user in.</summary>
public sealed record RegisterWithPasswordCommand(
    string Email,
    string Password,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? Locale,
    string ClientId,
    string? UserAgent) : ICommand<AuthTokens>;

internal sealed class RegisterWithPasswordValidator : AbstractValidator<RegisterWithPasswordCommand>
{
    public RegisterWithPasswordValidator()
    {
        RuleFor(c => c.Email).NotEmpty();
        RuleFor(c => c.Password).NotEmpty();
        RuleFor(c => c.DisplayName).NotEmpty();
        RuleFor(c => c.ClientId).NotEmpty();
    }
}

internal sealed class RegisterWithPasswordHandler(
    IIdentityRepository identities,
    IClientRegistry clients,
    ITokenIssuer tokens,
    IPasswordHasher hasher,
    IOptions<AuthOptions> options,
    TimeProvider time) : ICommandHandler<RegisterWithPasswordCommand, AuthTokens>
{
    public async Task<Result<AuthTokens>> Handle(RegisterWithPasswordCommand command, CancellationToken cancellationToken)
    {
        var client = clients.Resolve(command.ClientId, command.UserAgent);
        if (client.IsFailed)
        {
            return client.ToResult<AuthTokens>();
        }

        var email = Email.Create(command.Email);
        if (email.IsFailed)
        {
            return email.ToResult<AuthTokens>();
        }

        var profile = RegistrationProfile.Create(command.DisplayName, command.FirstName, command.LastName, null);
        if (profile.IsFailed)
        {
            return profile.ToResult<AuthTokens>();
        }

        var registration = Identity.RegisterWithPassword(
            email.Value,
            command.Password,
            profile.Value,
            new RegistrationContext(command.ClientId, client.Value.Info.UserAgent, command.Locale),
            client.Value.Info,
            await identities.EmailExistsAsync(email.Value, cancellationToken),
            options.Value.RegistrationOpen,
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
