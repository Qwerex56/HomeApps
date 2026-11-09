using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Members.ListMemberNames;

/// <summary>
/// C4 (chat plugin): who is in the household by name and avatar, for everyone with access, guests included, so a
/// plugin can show who wrote what. Nothing else: no roles, dates or who invited whom (the full list stays private, D2a).
/// </summary>
public sealed record ListMemberNamesQuery(Guid HouseholdId) : IQuery<IReadOnlyList<MemberNameView>>;
