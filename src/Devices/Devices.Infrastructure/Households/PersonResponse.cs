using HomeApps.Contracts.Households;

namespace Devices.Infrastructure.Households;

/// <summary>The part of Households' internal <c>GET /internal/households/{id}/members</c> Devices reads.</summary>
internal sealed record PersonResponse(Guid UserId, HouseholdRole Role);
