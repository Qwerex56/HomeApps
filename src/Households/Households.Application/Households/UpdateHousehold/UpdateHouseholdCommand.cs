using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Households;

/// <summary>H4: the whole form, name and description.</summary>
public sealed record UpdateHouseholdCommand(Guid HouseholdId, string Name, string? Description) : ICommand;
