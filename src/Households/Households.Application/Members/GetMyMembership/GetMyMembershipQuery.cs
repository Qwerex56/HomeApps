using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Members;

/// <summary>S5: my membership; 404 when I have none (Q3).</summary>
public sealed record GetMyMembershipQuery(Guid HouseholdId) : IQuery<MyMembershipView>;
