namespace HomeApps.Contracts.Accounts;

/// <summary>
/// An account was disabled (accounts.md 9). <see cref="BySelf"/> tells Auth whether a correct sign-in may restore it
/// (R16) or only an admin can. Auth ends every session.
/// </summary>
public sealed record UserDisabled(Guid UserId, bool BySelf) : IIntegrationEvent;

/// <summary>A disabled account is active again.</summary>
public sealed record UserEnabled(Guid UserId) : IIntegrationEvent;

/// <summary>The rare "forget me" hard erase. Auth deletes the identity with its logins and sessions.</summary>
public sealed record UserErased(Guid UserId) : IIntegrationEvent;
