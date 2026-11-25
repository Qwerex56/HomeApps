using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin;

/// <summary>UC-14: disable an account with an optional reason (R1, R3).</summary>
public sealed record DisableUserCommand(Guid UserId, string? Reason) : ICommand;
