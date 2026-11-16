using HomeMix.Application.Abstractions;

namespace HomeMix.Application.Store.GetMyListings;

/// <summary>
/// One of my store entries with its numbers: installations now and in households/on accounts, installs, updates and
/// removals, versions in use and installs a day for 30 days. Only numbers, never who installed it.
/// </summary>
public sealed record MyListingView(
    StoreEntryView Entry, StoreCounts Counts, IReadOnlyList<StoreVersionCount> Versions, IReadOnlyList<StoreDay> Days);
