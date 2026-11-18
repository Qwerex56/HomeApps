using HomeApps.BuildingBlocks.Application.Messaging;
using Households.Application.Common;

namespace Households.Application.Members.ListHouseholdPeople;

/// <summary>
/// H-0: the household's people for the JARVIS service (spoken PIN), who is not a member himself. No actor: the Api
/// authorizes the service client. Same rows as <see cref="ListMembersQuery"/>, people with access only.
/// </summary>
public sealed record ListHouseholdPeopleQuery(Guid HouseholdId) : IQuery<IReadOnlyList<MemberView>>;
