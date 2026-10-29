namespace HomeApps.Contracts.Auth;

/// <summary>The Owner changed a server role. Accounts updates its copy; the JWT carries it from the next refresh.</summary>
public sealed record IdentityRoleChanged(Guid UserId, ServerRole Role) : IIntegrationEvent;
