namespace HomeApps.Contracts.Accounts;

/// <summary>An account exists (accounts.md 9). Household keeps a copy of the name and avatar.</summary>
public sealed record UserCreated(Guid UserId, string DisplayName, string? AvatarUrl) : IIntegrationEvent;

/// <summary>
/// The name, avatar or time zone changed; the only profile data other services copy. <see cref="TimeZone"/> (IANA id) is
/// optional so an older publisher still works; Households uses it for homes without their own zone (release 3.0, 2.11).
/// </summary>
public sealed record UserProfileUpdated(Guid UserId, string DisplayName, string? AvatarUrl, string? TimeZone = null) : IIntegrationEvent;
