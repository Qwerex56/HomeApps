using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Admin;

/// <summary>A-22: Admin or Owner sends a reset link to someone with a lower role and ends their sessions (R10).</summary>
public sealed record ResetPasswordForCommand(Guid IdentityId) : ICommand;
