using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin;

/// <summary>UC-13: a higher role replaces someone's profile (R1 in the domain).</summary>
public sealed record UpdateUserProfileCommand(Guid UserId, ProfileInput Profile) : ICommand;
