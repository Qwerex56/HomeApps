using Auth.Application.Abstractions;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.MyAccount;

/// <summary>A-13: asks for a new email; the link goes to the new address and nothing changes until it is used (P8).</summary>
public sealed record RequestEmailChangeCommand(string NewEmail, string? CurrentPassword) : ICommand;

internal sealed class RequestEmailChangeValidator : AbstractValidator<RequestEmailChangeCommand>
{
    public RequestEmailChangeValidator() => RuleFor(c => c.NewEmail).NotEmpty();
}

internal sealed class RequestEmailChangeHandler(
    IIdentityRepository identities, ICurrentUser currentUser, IPasswordHasher hasher, TimeProvider time)
    : ICommandHandler<RequestEmailChangeCommand>
{
    public async Task<Result> Handle(RequestEmailChangeCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.NewEmail);
        if (email.IsFailed)
        {
            return email.ToResult();
        }

        var identity = await identities.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        if (identity is null)
        {
            return Result.Fail(IdentityErrors.CannotSignIn);
        }

        return identity.RequestEmailChange(
            command.CurrentPassword,
            currentUser.AuthenticatedAt ?? DateTimeOffset.MinValue,
            email.Value,
            await identities.EmailExistsAsync(email.Value, cancellationToken),
            hasher,
            time);
    }
}

/// <summary>A-13: the link from the email; R1 is checked again because the address may have been taken meanwhile.</summary>
public sealed record ConfirmEmailChangeCommand(string Token) : ICommand;

internal sealed class ConfirmEmailChangeValidator : AbstractValidator<ConfirmEmailChangeCommand>
{
    public ConfirmEmailChangeValidator() => RuleFor(c => c.Token).NotEmpty();
}

internal sealed class ConfirmEmailChangeHandler(IIdentityRepository identities, TimeProvider time)
    : ICommandHandler<ConfirmEmailChangeCommand>
{
    public async Task<Result> Handle(ConfirmEmailChangeCommand command, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByEmailTokenHashAsync(OpaqueToken.HashOf(command.Token), cancellationToken);
        if (identity?.EmailToken?.NewEmail is not { } newEmail)
        {
            return Result.Fail(IdentityErrors.TokenInvalidOrExpired);
        }

        var taken = await identities.EmailExistsAsync(newEmail, cancellationToken);
        return identity.ConfirmEmailChange(command.Token, taken, time);
    }
}
