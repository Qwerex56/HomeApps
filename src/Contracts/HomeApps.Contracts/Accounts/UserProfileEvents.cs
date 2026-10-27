namespace HomeApps.Contracts.Accounts;

/// <summary>An account exists (accounts.md 9). Household keeps a copy of the name and avatar.</summary>
public sealed record UserCreated(Guid UserId, string DisplayName, string? AvatarUrl) : IIntegrationEvent;

/// <summary>The name or avatar changed; the only profile data other services copy.</summary>
public sealed record UserProfileUpdated(Guid UserId, string DisplayName, string? AvatarUrl) : IIntegrationEvent;
