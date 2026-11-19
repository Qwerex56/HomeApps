using HomeApps.Contracts.Households;

namespace HomeMix.Infrastructure.Households;

/// <summary>One person of Households' <c>GET /internal/households/{id}/members</c>.</summary>
internal sealed record PersonResponse(Guid UserId, HouseholdRole Role);
