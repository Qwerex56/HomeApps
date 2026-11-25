using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.MyAccount;

/// <summary>UC-02: the whole profile form (Q5). Email and password are changed in Auth.</summary>
public sealed record UpdateMyProfileCommand(ProfileInput Profile) : ICommand;
