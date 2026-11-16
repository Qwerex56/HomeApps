using HomeMix.Domain.Packages;

namespace HomeMix.Domain.Store;

/// <summary>
/// One add-on in the store (homemix-2.md 2.1): a GitHub repository someone submitted, or an official one from
/// <c>addons.json</c>, with the package of its latest release. What the add-on is (name, icon, capabilities) is read
/// from that package's manifest; the entry keeps what GitHub says around it and whether it is shown.
/// </summary>
public sealed class StoreListing
{
    public const int MaxReadmeLength = 20_000;

    private StoreListing()
    {
    }

    public string Key { get; private set; } = string.Empty;

    public string AddonId { get; private set; } = string.Empty;

    /// <summary>https://github.com/owner/repo as GitHub names it.</summary>
    public string Repository { get; private set; } = string.Empty;

    /// <summary>The person who submitted it; <c>null</c> for an official entry.</summary>
    public Guid? SubmittedBy { get; private set; }

    public bool Official { get; private set; }

    public DateTimeOffset SubmittedAt { get; private set; }

    /// <summary>When its author last submitted it (the first time or again): what the daily limit counts.</summary>
    public DateTimeOffset LastSubmittedAt { get; private set; }

    public ListingStatus Status { get; private set; }

    /// <summary>The package of the latest release read: the version people install from the store.</summary>
    public string PackageSha256 { get; private set; } = string.Empty;

    public string ReleaseTag { get; private set; } = string.Empty;

    public DateTimeOffset? PublishedAt { get; private set; }

    public string? Readme { get; private set; }

    /// <summary>Why the last refresh failed (<c>no_release</c>, <c>id_changed</c>, a package error code...); the entry keeps its last good version.</summary>
    public string? Problem { get; private set; }

    public DateTimeOffset RefreshedAt { get; private set; }

    public static StoreListing Submit(AddonPackage package, string repository, Guid? submittedBy, StoreRelease release, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(repository);
        ArgumentNullException.ThrowIfNull(release);

        var listing = new StoreListing
        {
            Key = submittedBy is { } by ? ListingKey.For(package.AddonId, by, repository) : ListingKey.Official(package.AddonId),
            AddonId = package.AddonId,
            Repository = repository,
            SubmittedBy = submittedBy,
            Official = submittedBy is null,
            SubmittedAt = now,
            LastSubmittedAt = now,
            Status = ListingStatus.Listed,
        };
        listing.Take(package, release, now);
        return listing;
    }

    /// <summary>A new (or the same) latest release; the add-on id may not change, or it would be another add-on.</summary>
    public void Refresh(AddonPackage package, StoreRelease release, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(release);
        if (string.Equals(package.AddonId, AddonId, StringComparison.Ordinal))
        {
            Take(package, release, now);
        }
        else
        {
            Fail("id_changed", now);
        }
    }

    /// <summary>The latest release could not be read or used; the entry keeps its last good version.</summary>
    public void Fail(string problem, DateTimeOffset now)
    {
        Problem = problem;
        RefreshedAt = now;
    }

    /// <summary>Submitted again by its author: shown again after the author removed it (a hidden one stays hidden).</summary>
    public void Resubmit(DateTimeOffset now)
    {
        LastSubmittedAt = now;
        if (Status == ListingStatus.Removed)
        {
            Status = ListingStatus.Listed;
        }
    }

    public void Remove() => Status = ListingStatus.Removed;

    public void Hide(bool hidden)
    {
        if (Status != ListingStatus.Removed)
        {
            Status = hidden ? ListingStatus.Hidden : ListingStatus.Listed;
        }
    }

    public bool IsAuthor(Guid userId) => SubmittedBy == userId;

    private void Take(AddonPackage package, StoreRelease release, DateTimeOffset now)
    {
        PackageSha256 = package.Sha256;
        ReleaseTag = release.Tag;
        PublishedAt = release.PublishedAt;
        Readme = release.Readme is { Length: > MaxReadmeLength } readme ? readme[..MaxReadmeLength] + "\n\n…" : release.Readme;
        Problem = null;
        RefreshedAt = now;
    }
}
