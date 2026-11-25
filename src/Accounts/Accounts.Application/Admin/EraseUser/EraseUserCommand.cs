using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin;

/// <summary>
/// UC-17, R5: the Owner erases a disabled account for a real "forget me" request, confirmed by its email (Q1).
/// The user and its history go; an anonymous trace stays. <c>UserErased</c> reaches Auth and Household.
/// </summary>
public sealed record EraseUserCommand(Guid UserId, string ConfirmEmail) : ICommand;
