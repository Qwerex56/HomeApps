using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Members;

/// <summary>M4.</summary>
public sealed record ChangeMemberRoleCommand(Guid HouseholdId, Guid UserId, HouseholdRole Role) : ICommand;
