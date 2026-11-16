namespace HomeMix.Domain.Store;

/// <summary>Whether a store entry is shown: <see cref="Hidden"/> by the instance's Owner, <see cref="Removed"/> by its author.</summary>
public enum ListingStatus
{
    Listed,
    Hidden,
    Removed,
}
