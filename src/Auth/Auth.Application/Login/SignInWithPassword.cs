using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Login;

/// <summary>A-02 (and A-09 for an account the user disabled themselves).</summary>
public sealed record SignInWithPasswordCommand(string Email, string Password, string ClientId, string? UserAgent)
    : ICommand<SignInResult>;

internal sealed class SignInWithPasswordValidator : AbstractValidator<SignInWithPasswordCommand>
{
    public SignInWithPasswordValidator()
    {
        RuleFor(c => c.Email).NotEmpty();
        RuleFor(c => c.Password).NotEmpty();
        RuleFor(c => c.ClientId).NotEmpty();
    }
}

internal sealed class SignInWithPasswordHandler(
    IIdentityRepository identities,
    IClientRegistry clients,
    ITokenIssuer tokens,
    IPasswordHasher hasher,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ICommandHandler<SignInWithPasswordCommand, SignInResult>
{
    /// <summary>Hash of a password nobody has: unknown emails cost the same time as known ones (R4).</summary>
    private static string? _dummyHash;

    public async Task<Result<SignInResult>> Handle(SignInWithPasswordCommand command, CancellationToken cancellationToken)
    {
        var client = clients.Resolve(command.ClientId, command.UserAgent);
        if (client.IsFailed)
        {
            return client.ToResult<SignInResult>();
        }

        var email = Email.Create(command.Email);
        var identity = email.IsSuccess ? await identities.GetByEmailAsync(email.Value, cancellationToken) : null;
        if (identity is null)
        {
            _dummyHash ??= hasher.Hash(Guid.NewGuid().ToString());
            hasher.Verify(_dummyHash, command.Password);
            return Result.Fail<SignInResult>(IdentityErrors.InvalidCredentials);
        }

        var outcome = identity.SignInWithPassword(command.Password, client.Value.Info, hasher, time);
        if (outcome.IsFailed)
        {
            // P4: a wrong password counts towards the lockout, so it is saved although the command fails.
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return outcome.ToResult<SignInResult>();
        }

        return tokens.ToResult(outcome.Value, identity, client.Value.Registered);
    }
}
