using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Store;
using Microsoft.Extensions.Options;

namespace HomeMix.Application.Store.SubmitListing;

internal sealed class SubmitListingHandler(
    StoreCatalog catalog,
    IStoreListingRepository listings,
    ICurrentUser currentUser,
    TimeProvider time,
    IOptions<HomeMixOptions> options) : ICommandHandler<SubmitListingCommand, StoreEntryView>
{
    public async Task<Result<StoreEntryView>> Handle(SubmitListingCommand command, CancellationToken cancellationToken)
    {
        var me = currentUser.RequiredUserId;
        var now = time.GetUtcNow();
        var perDay = options.Value.StoreSubmissionsPerDay;
        // Checked before GitHub is asked, so the limit also limits the calls; a resubmission counts as one too.
        if (await listings.CountSubmittedSinceAsync(me, now.AddDays(-1), cancellationToken) >= perDay)
        {
            return Result.Fail(StoreErrors.TooMany(perDay));
        }

        var read = await catalog.ReadAsync(command.Repository, me, cancellationToken);
        if (read.IsFailed)
        {
            return read.ToResult();
        }

        var (package, release, repository) = read.Value;
        var listing = await listings.FindAsync(ListingKey.For(package.AddonId, me, repository), cancellationToken);
        if (listing is null)
        {
            listing = StoreListing.Submit(package, repository, me, release, now);
            listings.Add(listing);
        }
        else
        {
            listing.Resubmit(now);
            listing.Refresh(package, release, now);
        }

        return StoreEntryView.From(listing, package.Manifest, me);
    }
}
