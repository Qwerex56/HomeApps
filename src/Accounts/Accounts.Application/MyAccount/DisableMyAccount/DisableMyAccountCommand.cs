using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.MyAccount;

/// <summary>UC-03: I disable my account. Signing in again restores it (UC-05).</summary>
public sealed record DisableMyAccountCommand(string? Reason) : ICommand;
