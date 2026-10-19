using Auth.Domain.Common;
using HomeApps.BuildingBlocks.Domain;

namespace Auth.Domain.Invitations.Events;

/// <summary>Internal: sends the invitation email (A-34). Carries the raw token until the mail is sent (P5).</summary>
public sealed record InvitationCreated(
    Guid InvitationId,
    Email Email,
    string RawToken,
    InvitationCode Code,
    InvitationKind Kind,
    Email? InvitedByEmail,
    DateTimeOffset ExpiresAt) : IDomainEvent;
