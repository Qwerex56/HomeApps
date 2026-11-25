using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Households;

/// <summary>D5a: the new Owner closes the "you own this household now" banner.</summary>
public sealed record AcknowledgeOwnershipCommand(Guid HouseholdId) : ICommand;
