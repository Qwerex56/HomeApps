using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Store.GetMyListings;

/// <summary>The "Konto" tab (homemix-2.md 2.2): the caller's own store entries, listed or hidden, with their numbers.</summary>
public sealed record GetMyListingsQuery : IQuery<IReadOnlyList<MyListingView>>;
