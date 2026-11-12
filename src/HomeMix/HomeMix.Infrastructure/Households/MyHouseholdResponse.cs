using HomeApps.Contracts.Households;

namespace HomeMix.Infrastructure.Households;

/// <summary>The part of Households' <c>GET /households</c> HomeMix reads.</summary>
internal sealed record MyHouseholdResponse(Guid Id, string Name, HouseholdRole MyRole);
