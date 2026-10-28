using HomeApps.Contracts;

namespace Auth.Application.Events;

/// <summary>
/// Internal to Auth (Q6): one message for every email, written to the outbox with the change that caused it. Its
/// consumer builds the link from <see cref="Token"/> and sends the Polish template (A-34). Nobody outside Auth reads it.
/// </summary>
public sealed record AuthEmailRequested(
    AuthEmailTemplate Template,
    string To,
    string? Token,
    string? InvitationCode = null,
    string? InvitedByEmail = null,
    DateTimeOffset? ExpiresAt = null,
    Guid? HouseholdId = null,
    string? HouseholdName = null,
    bool PreviousOwnerErased = false) : IIntegrationEvent;

public enum AuthEmailTemplate
{
    Invitation = 0,

    /// <summary>A-30: the link is also written to the log, the server may have no SMTP yet.</summary>
    BootstrapInvitation = 1,
    PasswordReset = 2,
    PasswordChange = 3,
    EmailChange = 4,
    AccountReactivated = 5,

    /// <summary>Households D5a: the person became a household's Owner without choosing it (the Owner was banned or erased).</summary>
    HouseholdOwnershipReceived = 6,
}
