using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Options;

namespace Auth.Application.Refresh;

/// <summary>A-03: rotates the refresh token and issues a new access token with the current role.</summary>
public sealed record RefreshTokensCommand(string RefreshToken, string ClientId, string? UserAgent) : ICommand<AuthTokens>;

internal sealed class RefreshTokensValidator : AbstractValidator<RefreshTokensCommand>
{
    public RefreshTokensValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty();
        RuleFor(c => c.ClientId).NotEmpty();
    }
}

internal sealed class RefreshTokensHandler(
    IIdentityRepository identities,
    IClientRegistry clients,
    ITokenIssuer tokens,
    IUnitOfWork unitOfWork,
    IOptions<AuthOptions> options,
    TimeProvider time) : ICommandHandler<RefreshTokensCommand, AuthTokens>
{
    public async Task<Result<AuthTokens>> Handle(RefreshTokensCommand command, CancellationToken cancellationToken)
    {
        var client = clients.Resolve(command.ClientId, command.UserAgent);
        if (client.IsFailed)
        {
            return client.ToResult<AuthTokens>();
        }

        var identity = await identities.GetByRefreshTokenHashAsync(OpaqueToken.HashOf(command.RefreshToken), cancellationToken);
        if (identity is null)
        {
            return Result.Fail<AuthTokens>(IdentityErrors.InvalidRefreshToken);
        }

        var issued = identity.Refresh(command.RefreshToken, client.Value.Info, time, options.Value.RefreshGraceWindow);
        if (issued.IsFailed)
        {
            // P4: a reused token revokes its whole session (R7); that must be saved although the command fails.
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return issued.ToResult<AuthTokens>();
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Q8: another request used the same token at the same moment (two tabs). Not theft: the session stays, and
            // a retry with the same token within the grace window (R6a) gets a new one.
            return Result.Fail<AuthTokens>(IdentityErrors.InvalidRefreshToken);
        }

        return tokens.Tokens(identity, issued.Value, client.Value.Registered);
    }
}
