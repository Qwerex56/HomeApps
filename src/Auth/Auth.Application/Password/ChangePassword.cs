using Auth.Application.Abstractions;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Password;

/// <summary>
/// A-12: the signed-in user asks for a new password (also the first one for a Google-only account). Nothing changes
/// until the link from the email is used (P8). <paramref name="CurrentPassword"/> is required when the account has one (R8).
/// </summary>
public sealed record RequestPasswordChangeCommand(string? CurrentPassword, string NewPassword) : ICommand;

internal sealed class RequestPasswordChangeValidator : AbstractValidator<RequestPasswordChangeCommand>
{
    public RequestPasswordChangeValidator() => RuleFor(c => c.NewPassword).NotEmpty();
}

internal sealed class RequestPasswordChangeHandler(
    IIdentityRepository identities, ICurrentUser currentUser, IPasswordHasher hasher, TimeProvider time)
    : ICommandHandler<RequestPasswordChangeCommand>
{
    public async Task<Result> Handle(RequestPasswordChangeCommand command, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        if (identity is null)
        {
            return Result.Fail(IdentityErrors.CannotSignIn);
        }

        return identity.RequestPasswordChange(
            command.CurrentPassword, command.NewPassword, currentUser.AuthenticatedAt ?? DateTimeOffset.MinValue, hasher, time);
    }
}

/// <summary>A-12: the link from the email sets the waiting password and signs out everywhere (Q1).</summary>
public sealed record ConfirmPasswordChangeCommand(string Token) : ICommand;

internal sealed class ConfirmPasswordChangeValidator : AbstractValidator<ConfirmPasswordChangeCommand>
{
    public ConfirmPasswordChangeValidator() => RuleFor(c => c.Token).NotEmpty();
}

internal sealed class ConfirmPasswordChangeHandler(IIdentityRepository identities, TimeProvider time)
    : ICommandHandler<ConfirmPasswordChangeCommand>
{
    public async Task<Result> Handle(ConfirmPasswordChangeCommand command, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByEmailTokenHashAsync(OpaqueToken.HashOf(command.Token), cancellationToken);
        return identity is null
            ? Result.Fail(IdentityErrors.TokenInvalidOrExpired)
            : identity.ConfirmPasswordChange(command.Token, time);
    }
}
