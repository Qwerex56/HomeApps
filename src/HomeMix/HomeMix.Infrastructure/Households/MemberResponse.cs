using HomeApps.Contracts.Households;

namespace HomeMix.Infrastructure.Households;

/// <summary>The part of Households' <c>GET /households/{id}/members/</c> an add-on may see.</summary>
internal sealed record MemberResponse(Guid UserId, string DisplayName, string? AvatarUrl, HouseholdRole? Role, bool IsActive);
