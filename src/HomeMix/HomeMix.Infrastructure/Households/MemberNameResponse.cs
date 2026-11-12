namespace HomeMix.Infrastructure.Households;

/// <summary>The part of Households' <c>GET /households/{id}/members/names</c> HomeMix reads.</summary>
internal sealed record MemberNameResponse(Guid UserId);
