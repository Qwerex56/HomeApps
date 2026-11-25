using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Households;

/// <summary>H3: members with access and server admins (D8); everybody else gets 404 (Q3).</summary>
public sealed record GetHouseholdQuery(Guid HouseholdId) : IQuery<HouseholdView>;
