using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Admin;

/// <summary>A-23: the Owner grants or removes Admin (R18).</summary>
public sealed record ChangeRoleCommand(Guid IdentityId, ServerRole Role) : ICommand;
