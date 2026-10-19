namespace HomeApps.Contracts.Auth;

/// <summary>
/// A new identity exists (auth.md 8). Accounts creates the User from it; consumers must be idempotent on
/// <see cref="UserId"/> (R13). Profile and context are only carried here, Auth does not store them.
/// </summary>
public sealed record IdentityRegistered(
    Guid UserId,
    string Email,
    ServerRole Role,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    RegistrationSource Source,
    DateTimeOffset RegisteredAt,
    bool ViaReferral,
    string? InvitedByEmail,
    string? InvitedEmail,
    RegistrationContext Context) : IIntegrationEvent;

/// <summary>Where the sign-up came from. Never the IP address.</summary>
public sealed record RegistrationContext(string? ClientApp, string? UserAgent, string? Locale);
