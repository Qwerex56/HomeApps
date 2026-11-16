using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Store.GetStore;

/// <summary>The store: official add-ons first (built in, then addons.json), then submitted ones, newest first; no blocked, hidden or removed ones.</summary>
public sealed record GetStoreQuery : IQuery<IReadOnlyList<StoreEntryView>>;
