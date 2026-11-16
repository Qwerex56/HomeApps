using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Store.GetListing;

/// <summary>One store entry (<c>/store/{listingKey}</c>); a hidden one only for its author and the Owner.</summary>
public sealed record GetListingQuery(string Key) : IQuery<StoreEntryView>;
