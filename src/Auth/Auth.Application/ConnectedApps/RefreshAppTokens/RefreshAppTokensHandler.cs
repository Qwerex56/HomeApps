using Auth.Application.Abstractions;
using Auth.Domain.Common;
using Auth.Domain.ConnectedApps;
using Auth.Domain.Identities;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Options;

namespace Auth.Application.ConnectedApps.RefreshAppTokens;

internal sealed class RefreshAppTokensHandler(
    IAppClientRepository clients,
    IIdentityRepository identities,
    ITokenIssuer tokens,
    IUnitOfWork unitOfWork,
    IOptions<AuthOptions> options,
    TimeProvider time) : ICommandHandler<RefreshAppTokensCommand, AppTokens>
{
    public async Task<Result<AppTokens>> Handle(RefreshAppTokensCommand command, CancellationToken cancellationToken)
    {
        var client = await clients.GetByIdAsync(command.ClientId!, cancellationToken);
        var info = ClientInfo.Create(command.ClientId, command.UserAgent);
        if (client is null || info.IsFailed)
        {
            return Result.Fail<AppTokens>(ConnectedAppErrors.InvalidClient);
        }

        var hash = OpaqueToken.HashOf(command.RefreshToken!);
        var identity = await identities.GetByRefreshTokenHashAsync(hash, cancellationToken);
        var session = identity?.Sessions.FirstOrDefault(s => s.RefreshTokens.Any(t => t.TokenHash == hash));
        if (identity is null || session is not { IsConnectedApp: true })
        {
            return Result.Fail<AppTokens>(ConnectedAppErrors.InvalidGrant);
        }

        if (command.Resource is not null && !string.Equals(command.Resource.TrimEnd('/'), session.Audience!.TrimEnd('/'), StringComparison.Ordinal))
        {
            return Result.Fail<AppTokens>(ConnectedAppErrors.InvalidTarget);
        }

        var issued = identity.Refresh(command.RefreshToken!, info.Value, time, options.Value.RefreshGraceWindow);
        if (issued.IsFailed)
        {
            // A reused token ends the session (R7); that is saved although the request fails (P4).
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Fail<AppTokens>(ConnectedAppErrors.InvalidGrant);
        }

        var now = time.GetUtcNow();
        client.MarkUsed(now);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Two refreshes with one token at the same moment; a retry within the grace window gets a new one (R6a).
            return Result.Fail<AppTokens>(ConnectedAppErrors.InvalidGrant);
        }

        return tokens.IssueAppTokens(identity, issued.Value, client.Id, now);
    }
}
