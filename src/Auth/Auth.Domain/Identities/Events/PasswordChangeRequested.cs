using Auth.Domain.Common;
using HomeApps.BuildingBlocks.Domain;

namespace Auth.Domain.Identities.Events;

/// <summary>Internal: sends the link that confirms a password change (A-12, P8).</summary>
public sealed record PasswordChangeRequested(Guid IdentityId, Email Email, string RawToken) : IDomainEvent;
