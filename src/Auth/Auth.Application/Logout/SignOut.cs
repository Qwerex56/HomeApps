using Auth.Application.Abstractions;
using Auth.Domain.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Logout;

/// <summary>A-04: ends the session of this refresh token. Always succeeds, so a stale cookie can always be cleared.</summary>
public sealed record SignOutCommand(string RefreshToken) : ICommand;

internal sealed class SignOutValidator : AbstractValidator<SignOutCommand>
{
    public SignOutValidator() => RuleFor(c => c.RefreshToken).NotEmpty();
}

internal sealed class SignOutHandler(IIdentityRepository identities, TimeProvider time) : ICommandHandler<SignOutCommand>
{
    public async Task<Result> Handle(SignOutCommand command, CancellationToken cancellationToken)
    {
        var hash = OpaqueToken.HashOf(command.RefreshToken);
        var identity = await identities.GetByRefreshTokenHashAsync(hash, cancellationToken);
        var session = identity?.Sessions.FirstOrDefault(s => s.RefreshTokens.Any(t => t.TokenHash == hash));
        if (session is not null)
        {
            identity!.SignOut(session.Id, time);
        }

        return Result.Ok();
    }
}

/// <summary>A-14: ends every session of the caller, this one included.</summary>
public sealed record SignOutEverywhereCommand : ICommand;

internal sealed class SignOutEverywhereHandler(IIdentityRepository identities, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<SignOutEverywhereCommand>
{
    public async Task<Result> Handle(SignOutEverywhereCommand command, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        identity?.SignOutEverywhere(time);
        return Result.Ok();
    }
}
