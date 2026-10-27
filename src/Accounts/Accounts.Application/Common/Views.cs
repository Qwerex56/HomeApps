using Accounts.Domain.Common;
using Accounts.Domain.Users;

namespace Accounts.Application.Common;

public sealed record ProfileView(
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? PreferredName,
    string? AvatarUrl,
    string Locale,
    string TimeZone,
    DateOnly? DateOfBirth);

public sealed record RegistrationView(
    RegistrationSource Source,
    DateTimeOffset RegisteredAt,
    bool ViaReferral,
    string? InvitedByEmail,
    string? InvitedEmail,
    string? ClientApp,
    string? UserAgent,
    string? Locale);

/// <summary>Why the account is disabled. <see cref="ByUserId"/> is only filled for admins (accounts.md 3.2).</summary>
public sealed record DisablingView(DateTimeOffset At, bool BySelf, string? Reason, Guid? ByUserId);

/// <summary>UC-01: my account. No <c>DisabledBy</c> and no <c>LastSeenAt</c> (admin data).</summary>
public sealed record MyAccountView(
    Guid Id,
    string Email,
    ServerRole Role,
    AccountStatus Status,
    ProfileView Profile,
    DisablingView? Disabling,
    RegistrationView Registration,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>UC-11: everything from accounts.md 3 for an admin.</summary>
public sealed record UserView(
    Guid Id,
    string Email,
    ServerRole Role,
    AccountStatus Status,
    ProfileView Profile,
    DisablingView? Disabling,
    RegistrationView Registration,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastSeenAt);

/// <summary>UC-04, Q6: the user only sees when and what kind of change happened.</summary>
public sealed record MyHistoryEntryView(DateTimeOffset At, HistoryAction Action);

/// <summary>UC-16: the full audit entry. <see cref="ActorName"/> is null for changes from Auth or erased actors.</summary>
public sealed record HistoryEntryView(
    DateTimeOffset At,
    HistoryAction Action,
    Guid? ActorId,
    string? ActorName,
    string? Reason,
    string? Details);

internal static class Views
{
    public static ProfileView ToView(this Profile p) => new(
        p.DisplayName.Value,
        p.FirstName?.Value,
        p.LastName?.Value,
        p.PreferredName?.Value,
        p.AvatarUrl?.ToString(),
        p.Locale.Value,
        p.TimeZone.Value,
        p.DateOfBirth);

    public static RegistrationView ToView(this Registration r) => new(
        r.Source, r.RegisteredAt, r.ViaReferral, r.InvitedByEmail, r.InvitedEmail, r.ClientApp, r.UserAgent, r.Locale);

    public static DisablingView? DisablingView(this User u, bool forAdmin) => u.Disabling is { } d
        ? new DisablingView(d.At, u.IsDisabledBySelf, d.Reason, forAdmin ? d.ByUserId : null)
        : null;

    public static MyAccountView ToMyView(this User u) => new(
        u.Id, u.Email.Value, u.Role, u.Status, u.Profile.ToView(), u.DisablingView(forAdmin: false),
        u.Registration.ToView(), u.CreatedAt, u.UpdatedAt);

    public static UserView ToAdminView(this User u) => new(
        u.Id, u.Email.Value, u.Role, u.Status, u.Profile.ToView(), u.DisablingView(forAdmin: true),
        u.Registration.ToView(), u.CreatedAt, u.UpdatedAt, u.LastSeenAt);
}
