using Auth.Application.Abstractions;
using Auth.Domain.Common;
using Auth.Domain.ConnectedApps;
using Auth.Domain.Identities;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.ConnectedApps.RedeemCode;

internal sealed class RedeemCodeHandler(
    IAppClientRepository clients,
    IAuthorizationCodeRepository codes,
    IIdentityRepository identities,
    ITokenIssuer tokens,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ICommandHandler<RedeemCodeCommand, AppTokens>
{
    public async Task<Result<AppTokens>> Handle(RedeemCodeCommand command, CancellationToken cancellationToken)
    {
        var client = await clients.GetByIdAsync(command.ClientId!, cancellationToken);
        if (client is null)
        {
            return Result.Fail<AppTokens>(ConnectedAppErrors.InvalidClient);
        }

        var code = await codes.GetByHashAsync(OpaqueToken.HashOf(command.Code!), cancellationToken);
        if (code is null)
        {
            return Result.Fail<AppTokens>(ConnectedAppErrors.InvalidGrant);
        }

        var now = time.GetUtcNow();
        var alreadyUsed = code.UsedAt is not null;
        var redeemed = code.Redeem(client.Id, command.RedirectUri, command.CodeVerifier, now);
        if (redeemed.IsFailed)
        {
            if (alreadyUsed && code.SessionId is { } leaked)
            {
                // The code leaked: whoever got the first tokens loses them too (OAuth 2.1, 4.1.3).
                var owner = await identities.GetByIdAsync(code.IdentityId, cancellationToken);
                owner?.SignOut(leaked, time);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result.Fail<AppTokens>(ConnectedAppErrors.InvalidGrant);
        }

        if (command.Resource is not null && !string.Equals(command.Resource.TrimEnd('/'), code.Audience.TrimEnd('/'), StringComparison.Ordinal))
        {
            return Result.Fail<AppTokens>(ConnectedAppErrors.InvalidTarget);
        }

        var identity = await identities.GetByIdAsync(code.IdentityId, cancellationToken);
        var info = ClientInfo.Create(client.Id, command.UserAgent);
        var session = identity is null || info.IsFailed
            ? Result.Fail<IssuedRefreshToken>(IdentityErrors.CannotSignIn)
            : identity.ConnectApp(info.Value, code.Audience, time);
        if (session.IsFailed)
        {
            // The code is spent either way.
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Fail<AppTokens>(ConnectedAppErrors.InvalidGrant);
        }

        code.StartedSession(session.Value.SessionId);
        client.MarkUsed(now);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // The same code redeemed twice at the same moment: only the first one gets tokens.
            return Result.Fail<AppTokens>(ConnectedAppErrors.InvalidGrant);
        }

        return tokens.IssueAppTokens(identity!, session.Value, client.Id, now);
    }
}
