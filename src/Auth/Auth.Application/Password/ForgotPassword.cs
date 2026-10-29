using Auth.Application.Abstractions;
using Auth.Domain.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Password;

/// <summary>A-08: always succeeds (202), whether or not the email has an account (R9).</summary>
public sealed record ForgotPasswordCommand(string Email) : ICommand;

internal sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator() => RuleFor(c => c.Email).NotEmpty();
}

internal sealed class ForgotPasswordHandler(IIdentityRepository identities, TimeProvider time)
    : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        var identity = email.IsSuccess ? await identities.GetByEmailAsync(email.Value, cancellationToken) : null;
        identity?.RequestPasswordReset(time);
        return Result.Ok();
    }
}

/// <summary>A-08: the new password from the reset link.</summary>
public sealed record ResetPasswordCommand(string Token, string NewPassword) : ICommand;

internal sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(c => c.Token).NotEmpty();
        RuleFor(c => c.NewPassword).NotEmpty();
    }
}

internal sealed class ResetPasswordHandler(IIdentityRepository identities, IPasswordHasher hasher, TimeProvider time)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByEmailTokenHashAsync(OpaqueToken.HashOf(command.Token), cancellationToken);
        return identity is null
            ? Result.Fail(Domain.Identities.IdentityErrors.TokenInvalidOrExpired)
            : identity.ResetPassword(command.Token, command.NewPassword, hasher, time);
    }
}
