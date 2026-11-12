using HomeApps.Contracts.Households;

namespace HomeMix.Infrastructure.Households;

/// <summary>The part of Households' <c>GET /households/{id}/members/me</c> HomeMix reads.</summary>
internal sealed record MembershipResponse(HouseholdRole Role);
