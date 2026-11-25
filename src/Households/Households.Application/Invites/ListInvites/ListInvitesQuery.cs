using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Invites;

/// <summary>I2, D3a: invites still working; everyone's for the Owner and Admins, only my own for a member.</summary>
public sealed record ListInvitesQuery(Guid HouseholdId) : IQuery<IReadOnlyList<InviteView>>;
