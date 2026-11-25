using Households.Domain.Households;

namespace Households.Application.Members.ListMemberNames;

/// <param name="Role">W6: the role, so a guest can see who is in the household; never emails or invite data.</param>
/// <param name="IsActive">Always true here: only members with access are listed.</param>
/// <param name="Name">The nickname in this household, otherwise the display name.</param>
public sealed record MemberNameView(Guid UserId, string Name, string? AvatarUrl, HouseholdRole? Role = null, bool IsActive = true);
