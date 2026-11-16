using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store.GiveHeart;

internal sealed class GiveHeartHandler(
    IOfficialAddons builtin, IStoreListingRepository listings, IStoreStatsRepository stats, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<GiveHeartCommand>
{
    public async Task<Result> Handle(GiveHeartCommand command, CancellationToken cancellationToken)
    {
        var me = currentUser.RequiredUserId;
        var listed = builtin.List().Any(o => ListingKey.Official(o.Manifest.Id) == command.Key)
            || await listings.FindAsync(command.Key, cancellationToken) is { Status: ListingStatus.Listed };
        if (!listed)
        {
            return Result.Fail(StoreErrors.NotFound);
        }

        if (await stats.FindHeartAsync(command.Key, me, cancellationToken) is null)
        {
            stats.Add(StoreHeart.Give(command.Key, me, time.GetUtcNow()));
        }

        return Result.Ok();
    }
}
