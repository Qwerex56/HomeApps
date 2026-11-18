using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Housekeeping.PruneEvents;

/// <summary>The daily cleanup (D9): events older than their household's retention go. Returns how many.</summary>
public sealed record PruneEventsCommand : ICommand<int>;
