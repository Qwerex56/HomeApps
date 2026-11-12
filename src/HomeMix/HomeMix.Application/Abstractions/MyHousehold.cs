using HomeApps.Contracts.Households;

namespace HomeMix.Application.Abstractions;

/// <summary>A household the caller can reach, with its name (how a person names it to JARVIS) and the caller's role.</summary>
public sealed record MyHousehold(Guid Id, string Name, HouseholdRole Role);
