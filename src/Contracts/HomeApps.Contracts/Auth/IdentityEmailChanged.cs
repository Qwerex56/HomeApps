namespace HomeApps.Contracts.Auth;

/// <summary>The user confirmed a new email address. Accounts updates its copy.</summary>
public sealed record IdentityEmailChanged(Guid UserId, string Email) : IIntegrationEvent;
