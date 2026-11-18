using HomeApps.Contracts.Households;

namespace Devices.Infrastructure.Households;

/// <summary>The part of Households' <c>GET /households/{id}/members/me</c> Devices reads.</summary>
internal sealed record MembershipResponse(HouseholdRole Role);
