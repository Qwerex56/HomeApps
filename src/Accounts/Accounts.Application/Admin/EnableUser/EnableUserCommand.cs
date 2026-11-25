using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin;

/// <summary>UC-14: enable any disabled account below the caller's role (R1).</summary>
public sealed record EnableUserCommand(Guid UserId) : ICommand;
