using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store.HideListing;

internal sealed class HideListingHandler(IStoreListingRepository listings, IPackageRepository packages, ICurrentUser currentUser)
    : ICommandHandler<HideListingCommand, StoreEntryView>
{
    public async Task<Result<StoreEntryView>> Handle(HideListingCommand command, CancellationToken cancellationToken)
    {
        var listing = await listings.FindAsync(command.Key, cancellationToken);
        if (listing is null || listing.Status == ListingStatus.Removed)
        {
            return Result.Fail(StoreErrors.NotFound);
        }

        listing.Hide(command.Hidden);
        return await StoreEntries.ViewAsync(listing, packages, currentUser.UserId, cancellationToken);
    }
}
