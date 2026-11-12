using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Store.GetStore;

/// <summary>The store: the official add-ons first, then the index's, without the blocked ones.</summary>
public sealed record GetStoreQuery : IQuery<IReadOnlyList<StoreEntryView>>;
