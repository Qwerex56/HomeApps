using HomeApps.Contracts.Households;

namespace HomeMix.Application.Abstractions;

/// <summary>An active member as an add-on may see them (the bridge's <c>members()</c>): never an email.</summary>
public sealed record HouseholdMember(Guid Id, string Name, string? AvatarUrl, HouseholdRole Role);
