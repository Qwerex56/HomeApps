using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;

namespace HomeMix.Application.Store.TakeHeart;

internal sealed class TakeHeartHandler(IStoreStatsRepository stats, ICurrentUser currentUser) : ICommandHandler<TakeHeartCommand>
{
    public async Task<Result> Handle(TakeHeartCommand command, CancellationToken cancellationToken)
    {
        var me = currentUser.RequiredUserId;
        if (await stats.FindHeartAsync(command.Key, me, cancellationToken) is { } heart)
        {
            stats.Remove(heart);
        }

        return Result.Ok();
    }
}
