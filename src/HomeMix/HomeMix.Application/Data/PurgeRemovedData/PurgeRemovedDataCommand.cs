using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Data.PurgeRemovedData;

/// <summary>Deletes the data of add-ons uninstalled longer than <see cref="DataRetention.AfterRemoval"/> ago; answers how many documents.</summary>
public sealed record PurgeRemovedDataCommand : ICommand<int>;
