namespace HomeMix.Domain.Store;

/// <summary>A "domek" (homemix-2.md 2.2): one person likes one store entry, once.</summary>
public sealed class StoreHeart
{
    private StoreHeart()
    {
    }

    public string ListingKey { get; private set; } = string.Empty;

    public Guid UserId { get; private set; }

    public DateTimeOffset At { get; private set; }

    public static StoreHeart Give(string listingKey, Guid userId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(listingKey);
        return new StoreHeart { ListingKey = listingKey, UserId = userId, At = now };
    }
}
