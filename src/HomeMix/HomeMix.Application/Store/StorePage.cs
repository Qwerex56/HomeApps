namespace HomeMix.Application.Store;

/// <summary>How the store lists entries (homemix-2.md 2.4): its tabs and page size.</summary>
public static class StorePage
{
    public const int Size = 24;

    public const string Featured = "featured";

    public const string Popular = "popular";

    public const string Hearts = "hearts";

    public const string New = "new";

    public static IReadOnlyList<string> Tabs { get; } = [Featured, Popular, Hearts, New];
}
