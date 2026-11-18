using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Telemetry.GetStreamAccess;

/// <summary>May the caller open the household's stream, and does it include events (members) or only state (guests, D9)?</summary>
public sealed record GetStreamAccessQuery(Guid HouseholdId) : IQuery<bool>;
