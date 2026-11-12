using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Blocks;
using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Store.GetStore;

internal sealed class GetStoreHandler(IOfficialAddons official, IStoreIndex index, Blocklist blocklist)
    : IQueryHandler<GetStoreQuery, IReadOnlyList<StoreEntryView>>
{
    private const string CommunityIcon = "pi-box";

    public async Task<Result<IReadOnlyList<StoreEntryView>>> Handle(GetStoreQuery query, CancellationToken cancellationToken)
    {
        var blocked = await blocklist.SnapshotAsync(cancellationToken);

        var entries = official.List()
            .Where(o => !blocked(o.Manifest.Id, o.Sha256))
            .Select(o => new StoreEntryView(
                true, o.Manifest.Id, null, o.Manifest.Name, o.Manifest.Description, o.Manifest.Icon, o.Manifest.Version,
                o.Sha256, null, null, null))
            .ToList();

        foreach (var listing in await index.ListAsync(cancellationToken))
        {
            // The id is only known after downloading, so a store entry hides only by its hash.
            if (listing.Sha256 is { } sha && blocked(string.Empty, sha))
            {
                continue;
            }

            var name = new LocalizedText(listing.Title, listing.Title);
            var description = listing.Description is null ? null : new LocalizedText(listing.Description, listing.Description);
            entries.Add(new StoreEntryView(
                false, null, listing.Repository, name, description, CommunityIcon, listing.Version, listing.Sha256,
                listing.Readme, listing.PublishedAt, listing.Problem));
        }

        return entries;
    }
}
