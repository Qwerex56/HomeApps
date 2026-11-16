using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Store.HideListing;

/// <summary>The instance's Owner hides an entry from the store, or shows it again (Owner policy on the endpoint).</summary>
public sealed record HideListingCommand(string Key, bool Hidden) : ICommand<StoreEntryView>;
