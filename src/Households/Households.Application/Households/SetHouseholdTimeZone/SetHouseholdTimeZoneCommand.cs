using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Households;

/// <summary>2.11: the Owner or an Admin gives the household its own time zone (IANA id).</summary>
public sealed record SetHouseholdTimeZoneCommand(Guid HouseholdId, string TimeZone) : ICommand;
