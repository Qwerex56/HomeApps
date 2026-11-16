using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Store.ListAllListings;

/// <summary>The Owner's list of store entries (homemix-2.md 2.2): listed and hidden ones, with numbers, to hide or show again.</summary>
public sealed record ListAllListingsQuery : IQuery<IReadOnlyList<StoreEntryView>>;
