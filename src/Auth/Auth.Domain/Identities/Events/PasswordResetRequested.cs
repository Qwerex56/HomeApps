using Auth.Domain.Common;
using HomeApps.BuildingBlocks.Domain;

namespace Auth.Domain.Identities.Events;

/// <summary>Internal: sends the password reset email (A-08, A-22). Carries the raw token until the mail is sent (P5).</summary>
public sealed record PasswordResetRequested(Guid IdentityId, Email Email, string RawToken) : IDomainEvent;
