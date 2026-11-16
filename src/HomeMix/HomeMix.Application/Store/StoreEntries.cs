using FluentResults;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store;

/// <summary>Store entries with their packages' manifests, as the API shows them.</summary>
internal static class StoreEntries
{
    public static async Task<Result<StoreEntryView>> ViewAsync(
        StoreListing listing, IPackageRepository packages, Guid? me, CancellationToken cancellationToken)
    {
        var package = await packages.FindAsync(listing.PackageSha256, cancellationToken);
        return package is null ? Result.Fail(StoreErrors.NotFound) : StoreEntryView.From(listing, package.Manifest, me);
    }

    /// <summary>The entry, when the caller may change it: its author or the instance's Owner; official ones only the Owner.</summary>
    public static async Task<Result<StoreListing>> FindForAuthorAsync(
        string key, IStoreListingRepository listings, Guid me, bool owner, CancellationToken cancellationToken)
    {
        var listing = await listings.FindAsync(key, cancellationToken);
        if (listing is null || listing.Status == ListingStatus.Removed)
        {
            return Result.Fail(StoreErrors.NotFound);
        }

        return listing.IsAuthor(me) || owner ? listing : Result.Fail(StoreErrors.NotAuthor);
    }
}
