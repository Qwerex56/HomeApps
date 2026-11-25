using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Members;

/// <summary>M6 and D2a: my nickname, or a guest's set by the Owner or an Admin. Blank clears it (Q4).</summary>
public sealed record SetNicknameCommand(Guid HouseholdId, Guid UserId, string? Nickname) : ICommand;
