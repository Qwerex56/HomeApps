using Auth.Domain.Common;
using HomeApps.BuildingBlocks.Domain;

namespace Auth.Domain.Identities.Events;

/// <summary>
/// A new identity exists. Becomes <c>HomeApps.Contracts.IdentityRegistered</c> for Accounts, which creates the User.
/// The profile and context are not stored by Auth; they only travel here.
/// </summary>
public sealed record IdentityRegistered(
    Guid IdentityId,
    Email Email,
    ServerRole Role,
    RegistrationSource Source,
    RegistrationProfile Profile,
    RegistrationContext Context,
    Email? InvitedByEmail,
    Email? InvitedEmail,
    DateTimeOffset RegisteredAt) : IDomainEvent;
