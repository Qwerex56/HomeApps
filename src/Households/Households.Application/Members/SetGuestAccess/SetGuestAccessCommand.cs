using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Members;

/// <summary>D2c: when a guest's access ends; null = no end.</summary>
public sealed record SetGuestAccessCommand(Guid HouseholdId, Guid UserId, DateTimeOffset? Until) : ICommand;
