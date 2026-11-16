using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Store.RefreshStore;

/// <summary>
/// HomeMix's own round (every few minutes, no caller): lists new repositories of addons.json as official entries and
/// reads again the entries not read for <c>HomeMix:StoreRefreshMinutes</c>, a few per round. Answers how many it read.
/// </summary>
public sealed record RefreshStoreCommand : ICommand<int>;
