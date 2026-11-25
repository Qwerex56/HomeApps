using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Households;

/// <summary>D2b: a new guest link (the old one stops working); returns the code as people see it.</summary>
public sealed record RegenerateGuestLinkCommand(Guid HouseholdId) : ICommand<string>;
