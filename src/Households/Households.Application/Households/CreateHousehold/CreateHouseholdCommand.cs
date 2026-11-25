using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Households;

/// <summary>H1: the caller creates a household and becomes its Owner. A disabled account cannot (Q5).</summary>
public sealed record CreateHouseholdCommand(string Name, string? Description) : ICommand<Guid>;
