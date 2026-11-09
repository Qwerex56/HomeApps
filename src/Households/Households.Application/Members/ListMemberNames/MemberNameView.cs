namespace Households.Application.Members.ListMemberNames;

/// <param name="Name">The nickname in this household, otherwise the display name.</param>
public sealed record MemberNameView(Guid UserId, string Name, string? AvatarUrl);
