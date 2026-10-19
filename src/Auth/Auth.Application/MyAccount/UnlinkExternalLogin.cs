using Auth.Application.Abstractions;
using Auth.Domain.Identities;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.MyAccount;

/// <summary>A-11: removes Google or Apple from my account, unless it is my last way of signing in (R12).</summary>
public sealed record UnlinkExternalLoginCommand(ExternalProvider Provider) : ICommand;

internal sealed class UnlinkExternalLoginValidator : AbstractValidator<UnlinkExternalLoginCommand>
{
    public UnlinkExternalLoginValidator() => RuleFor(c => c.Provider).IsInEnum();
}

internal sealed class UnlinkExternalLoginHandler(IIdentityRepository identities, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<UnlinkExternalLoginCommand>
{
    public async Task<Result> Handle(UnlinkExternalLoginCommand command, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        return identity is null
            ? Result.Fail(IdentityErrors.CannotSignIn)
            : identity.UnlinkExternalLogin(command.Provider, time);
    }
}
