using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Store;
using Microsoft.Extensions.Options;

namespace HomeMix.Application.Store.RefreshStore;

internal sealed class RefreshStoreHandler(
    StoreCatalog catalog,
    IStoreIndex index,
    IStoreListingRepository listings,
    IOfficialAddons builtin,
    TimeProvider time,
    IOptions<HomeMixOptions> options) : ICommandHandler<RefreshStoreCommand, int>
{
    /// <summary>Entries read per round: two GitHub calls each keeps a round within GitHub's anonymous limit of 60 an hour.</summary>
    public const int PerRound = 10;

    public async Task<Result<int>> Handle(RefreshStoreCommand command, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var read = 0;

        // New repositories of addons.json. One that cannot be read yet is tried again next round.
        var images = builtin.List().Select(o => o.Manifest.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var repository in await index.RepositoriesAsync(cancellationToken))
        {
            if (read >= PerRound || await listings.FindOfficialAsync(repository, cancellationToken) is not null)
            {
                continue;
            }

            read++;
            var release = await catalog.ReadAsync(repository, Guid.Empty, cancellationToken);
            // The same add-on built into the image wins (it needs no GitHub).
            if (release.IsSuccess && !images.Contains(release.Value.Package.AddonId)
                && await listings.FindAsync(ListingKey.Official(release.Value.Package.AddonId), cancellationToken) is null)
            {
                listings.Add(StoreListing.Submit(release.Value.Package, release.Value.Repository, null, release.Value.Release, now));
            }
        }

        var due = now.AddMinutes(-options.Value.StoreRefreshMinutes);
        foreach (var listing in await listings.ListDueAsync(due, Math.Max(0, PerRound - read), cancellationToken))
        {
            read++;
            await catalog.RefreshAsync(listing, Guid.Empty, cancellationToken);
        }

        return read;
    }
}
