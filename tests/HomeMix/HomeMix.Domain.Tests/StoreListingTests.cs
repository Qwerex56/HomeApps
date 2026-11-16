using HomeMix.Domain.Packages;
using HomeMix.Domain.Store;

namespace HomeMix.Domain.Tests;

public sealed class StoreListingTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    private static AddonPackage Package(string id, string version) =>
        AddonPackage.Create(
            Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(id + version))),
            Manifests.Valid(id: id, version: version),
            [new PackageFile("homemix.json", 100), new PackageFile("index.html", 10)],
            1024, "https://github.com/a/b", "v" + version, Guid.NewGuid(), Now).Value;

    [Fact]
    public void The_key_is_the_id_and_a_hash_of_the_person_and_the_repository()
    {
        var ewa = Guid.NewGuid();

        var key = ListingKey.For("chores", ewa, "https://github.com/Ewa/Chores");

        Assert.Matches("^chores-[0-9a-f]{8}$", key);
        Assert.Equal(key, ListingKey.For("chores", ewa, "https://github.com/ewa/chores"));
        Assert.NotEqual(key, ListingKey.For("chores", Guid.NewGuid(), "https://github.com/ewa/chores"));
        Assert.Equal("chores-official", ListingKey.Official("chores"));
    }

    [Fact]
    public void A_new_release_of_another_addon_keeps_the_last_good_version_with_a_problem()
    {
        var listing = StoreListing.Submit(Package("chores", "1.0.0"), "https://github.com/a/b", Guid.NewGuid(), new StoreRelease("v1", Now, "# A"), Now);

        listing.Refresh(Package("other", "2.0.0"), new StoreRelease("v2", Now, null), Now.AddHours(1));

        Assert.Equal("v1", listing.ReleaseTag);
        Assert.Equal("id_changed", listing.Problem);
        Assert.False(listing.Official);
    }

    [Fact]
    public void Removed_comes_back_on_resubmitting_but_hidden_stays_hidden()
    {
        var listing = StoreListing.Submit(Package("chores", "1.0.0"), "https://github.com/a/b", Guid.NewGuid(), new StoreRelease("v1", Now, null), Now);

        listing.Hide(true);
        listing.Resubmit(Now);
        var hidden = listing.Status;
        listing.Hide(false);
        listing.Remove();
        listing.Hide(false);
        var removed = listing.Status;
        listing.Resubmit(Now);

        Assert.Equal(ListingStatus.Hidden, hidden);
        Assert.Equal(ListingStatus.Removed, removed);
        Assert.Equal(ListingStatus.Listed, listing.Status);
    }
}
