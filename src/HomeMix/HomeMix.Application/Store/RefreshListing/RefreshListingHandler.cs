using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;

namespace HomeMix.Application.Store.RefreshListing;

internal sealed class RefreshListingHandler(
    StoreCatalog catalog, IStoreListingRepository listings, IPackageRepository packages, ICurrentUser currentUser)
    : ICommandHandler<RefreshListingCommand, StoreEntryView>
{
    public async Task<Result<StoreEntryView>> Handle(RefreshListingCommand command, CancellationToken cancellationToken)
    {
        var me = currentUser.RequiredUserId;
        var found = await StoreEntries.FindForAuthorAsync(command.Key, listings, me, currentUser.IsInRole("Owner"), cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult();
        }

        // A problem is kept on the entry and shown, not answered as an error: the entry still has its last good version.
        await catalog.RefreshAsync(found.Value, me, cancellationToken);
        return await StoreEntries.ViewAsync(found.Value, packages, me, cancellationToken);
    }
}
