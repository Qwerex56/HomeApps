using HomeApps.Contracts.Households;

namespace Devices.Infrastructure.Households;

/// <summary>The part of Households' <c>GET /households</c> Devices reads.</summary>
internal sealed record MyHouseholdResponse(Guid Id, HouseholdRole MyRole);
