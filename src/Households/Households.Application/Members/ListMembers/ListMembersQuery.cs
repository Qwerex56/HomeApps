using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Members;

/// <summary>
/// M1: members and up and server admins; a guest gets 403 (D2a: the list is private), a stranger 404 (Q3).
/// Owner first, then by role and join date.
/// </summary>
public sealed record ListMembersQuery(Guid HouseholdId) : IQuery<IReadOnlyList<MemberView>>;
