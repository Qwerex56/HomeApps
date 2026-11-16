using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Store.GetStore;

/// <summary>
/// The store (homemix-2.md 2.2): no blocked, hidden or removed entries. Without <see cref="Tab"/> official add-ons first (built in, then
/// addons.json), then submitted ones, newest first; <c>featured</c> only official ones, <c>popular</c> by active installations,
/// <c>hearts</c> by "domki", <c>new</c> by submission. <see cref="Q"/> searches name and description, <see cref="Target"/>
/// (<c>household</c>, <c>user</c>), <see cref="Jarvis"/> and <see cref="Offline"/> filter. <see cref="StorePage.Size"/>
/// entries a page, from page 1; a shorter page is the last one.
/// </summary>
public sealed record GetStoreQuery(
    string? Tab = null, string? Q = null, string? Target = null, bool Jarvis = false, bool Offline = false, int Page = 1)
    : IQuery<IReadOnlyList<StoreEntryView>>;
