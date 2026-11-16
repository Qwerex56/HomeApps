using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Store.RefreshListing;

/// <summary>"Odśwież teraz": the author (or the Owner) reads the entry's latest release now instead of within the hour.</summary>
public sealed record RefreshListingCommand(string Key) : ICommand<StoreEntryView>;
