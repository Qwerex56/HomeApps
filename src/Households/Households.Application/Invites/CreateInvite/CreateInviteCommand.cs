using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Invites;

/// <summary>I1, D3: my own invite link, Member or Guest, valid for a chosen time; a guest invite may end the guest's access (D2c).</summary>
public sealed record CreateInviteCommand(
    Guid HouseholdId, HouseholdRole Role, InviteValidity Validity, DateTimeOffset? GuestAccessUntil) : ICommand<InviteView>;
