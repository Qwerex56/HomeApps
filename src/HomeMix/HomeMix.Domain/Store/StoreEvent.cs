using HomeMix.Domain.Installations;
using HomeMix.Domain.Packages;

namespace HomeMix.Domain.Store;

/// <summary>
/// One install, update or removal for the store's numbers (homemix-2.md 2.2, S4): what was installed and from where,
/// never by whom or where. A zip upload has no source and is not counted.
/// </summary>
public sealed class StoreEvent
{
    private StoreEvent()
    {
    }

    public long Id { get; private set; }

    public Guid InstallationId { get; private set; }

    public string AddonId { get; private set; } = string.Empty;

    /// <summary><see cref="SourceOf"/>: the repository, or <see cref="ListingKey.OfficialSource"/> for a built-in add-on.</summary>
    public string Source { get; private set; } = string.Empty;

    public string Version { get; private set; } = string.Empty;

    public StoreEventKind Kind { get; private set; }

    public TargetKind TargetKind { get; private set; }

    public DateTimeOffset At { get; private set; }

    /// <summary>The event, or null for a package that is in no store (a zip).</summary>
    public static StoreEvent? Of(Installation installation, AddonPackage package, StoreEventKind kind, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(installation);
        ArgumentNullException.ThrowIfNull(package);
        return SourceOf(package) is { } source
            ? new StoreEvent
            {
                InstallationId = installation.Id,
                AddonId = package.AddonId,
                Source = source,
                Version = package.Version,
                Kind = kind,
                TargetKind = installation.TargetKind,
                At = now,
            }
            : null;
    }

    /// <summary>Where a package comes from for the store: its repository, the image for a built-in one, otherwise nothing.</summary>
    public static string? SourceOf(AddonPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        return package.Repository ?? (package.IsOfficial ? ListingKey.OfficialSource : null);
    }
}
