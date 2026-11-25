using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Invites;

/// <summary>I5 and D2b: join through an invite or the guest link; returns the household id. A disabled account cannot (Q5).</summary>
public sealed record AcceptInviteCommand(string Code) : ICommand<Guid>;
