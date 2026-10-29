namespace HomeApps.Contracts.Auth;

/// <summary>A sign-in or (at most every 15 minutes) a refresh. Accounts sets <c>LastSeenAt</c> from it.</summary>
public sealed record IdentitySignedIn(Guid UserId, DateTimeOffset At, SignInMethod Method, string? ClientApp) : IIntegrationEvent;
