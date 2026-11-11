using Auth.Application.Abstractions;
using Auth.Domain.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.ConnectedApps.RevokeAppToken;

internal sealed class RevokeAppTokenHandler(IIdentityRepository identities, IUnitOfWork unitOfWork, TimeProvider time)
    : ICommandHandler<RevokeAppTokenCommand>
{
    public async Task<Result> Handle(RevokeAppTokenCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.Token))
        {
            return Result.Ok();
        }

        var hash = OpaqueToken.HashOf(command.Token);
        var identity = await identities.GetByRefreshTokenHashAsync(hash, cancellationToken);
        var session = identity?.Sessions.FirstOrDefault(s => s.RefreshTokens.Any(t => t.TokenHash == hash));
        if (identity is null || session is not { IsConnectedApp: true }
            || (command.ClientId is not null && session.Client.ClientId != command.ClientId))
        {
            return Result.Ok();
        }

        identity.SignOut(session.Id, time);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
