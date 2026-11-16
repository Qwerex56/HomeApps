using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Store.SubmitListing;

/// <summary>
/// Lists a GitHub repository's add-on in the store (homemix-2.md 2.1): any signed-in person, a few a day. The same person
/// with the same repository gets the same entry, so submitting again reads it again (and shows it again if they removed it).
/// </summary>
public sealed record SubmitListingCommand(string Repository) : ICommand<StoreEntryView>;
