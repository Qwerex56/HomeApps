using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Store.RemoveListing;

/// <summary>Takes an entry out of the store (its author, or the Owner). Installations stay; submitting again lists it again.</summary>
public sealed record RemoveListingCommand(string Key) : ICommand;
