using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.External;

/// <summary>
/// A-11: a signed-in user adds Google or Apple. The callback is a browser redirect without the access token, so
/// <paramref name="IdentityId"/> comes from the OAuth state the Api signed when the flow started, never from user input.
/// </summary>
public sealed record LinkExternalLoginCommand(
    Guid IdentityId, ExternalProvider Provider, string Code, string CodeVerifier, Uri RedirectUri) : ICommand;

internal sealed class LinkExternalLoginValidator : AbstractValidator<LinkExternalLoginCommand>
{
    public LinkExternalLoginValidator()
    {
        RuleFor(c => c.IdentityId).NotEmpty();
        RuleFor(c => c.Provider).IsInEnum();
        RuleFor(c => c.Code).NotEmpty();
        RuleFor(c => c.CodeVerifier).NotEmpty();
        RuleFor(c => c.RedirectUri).NotNull();
    }
}

internal sealed class LinkExternalLoginHandler(
    IIdentityRepository identities,
    IExternalIdentityVerifier verifier,
    TimeProvider time) : ICommandHandler<LinkExternalLoginCommand>
{
    public async Task<Result> Handle(LinkExternalLoginCommand command, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByIdAsync(command.IdentityId, cancellationToken);
        if (identity is null || !identity.CanSignIn(time.GetUtcNow()))
        {
            return Result.Fail(IdentityErrors.CannotSignIn);
        }

        var external = await verifier.VerifyAsync(
            command.Provider, command.Code, command.CodeVerifier, command.RedirectUri, cancellationToken);
        if (external.IsFailed)
        {
            return external.ToResult();
        }

        // The provider's email is only recorded with the login, so it need not match or be verified here.
        var email = Email.Create(external.Value.Email);
        if (email.IsFailed)
        {
            return email.ToResult();
        }

        var loginTaken = await identities.ExternalLoginExistsAsync(command.Provider, external.Value.Subject, cancellationToken);
        return identity.LinkExternalLogin(command.Provider, external.Value.Subject, email.Value, loginTaken, time);
    }
}
