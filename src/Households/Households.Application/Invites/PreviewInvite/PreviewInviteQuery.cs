using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Invites;

/// <summary>
/// I4: what a link leads to, for anyone signed in. One link format for both (Q1): an invite code first, then the
/// household's guest link (D2b). Unknown, expired or revoked: 404, nothing is revealed (Q3).
/// </summary>
public sealed record PreviewInviteQuery(string Code) : IQuery<InvitePreviewView>;
