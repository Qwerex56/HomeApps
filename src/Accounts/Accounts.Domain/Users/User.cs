using Accounts.Domain.Common;
using Accounts.Domain.Users.Events;
using HomeApps.BuildingBlocks.Domain;

namespace Accounts.Domain.Users;

/// <summary>
/// The master account (accounts.md 4): profile, state, the copies of email and role from Auth, how it was created and
/// its history. Created only from Auth's <c>IdentityRegistered</c>, with the id Auth gave. Never deleted in the normal
/// flow, only disabled; <see cref="Erase"/> is the rare "forget me" exception.
/// </summary>
public sealed partial class User : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly List<HistoryEntry> _history = [];

    private User() { }

    private User(Guid id, Email email, ServerRole role, Profile profile, Registration registration, DateTimeOffset now)
    {
        Id = id;
        Email = email;
        Role = role;
        Profile = profile;
        Registration = registration;
        Status = AccountStatus.Active;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    /// <summary>Read-only copy from Auth (D2); changed only by <see cref="SyncEmail"/>.</summary>
    public Email Email { get; private set; } = null!;

    /// <summary>Copy of the role owned by Auth (A-23); changed only by <see cref="SyncRole"/>.</summary>
    public ServerRole Role { get; private set; }

    public Profile Profile { get; private set; } = null!;

    public AccountStatus Status { get; private set; }

    /// <summary>Set while <see cref="Status"/> is <see cref="AccountStatus.Disabled"/>.</summary>
    public Disabling? Disabling { get; private set; }

    public Registration Registration { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Last sign-in or refresh seen in Auth (A-35); for admins spotting unused accounts. Not in the history.</summary>
    public DateTimeOffset? LastSeenAt { get; private set; }

    public IReadOnlyList<HistoryEntry> History => _history;

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// UC-08, UC-09: the account for a new identity from Auth, with the role Auth gave (<c>Owner</c> for the bootstrap).
    /// Idempotency (R7) is the handler's job: it checks the id first.
    /// </summary>
    public static User Register(Guid id, Email email, ServerRole role, Profile profile, Registration registration, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(time);
        if (id == Guid.Empty)
        {
            throw new ArgumentException("The user id comes from Auth and cannot be empty.", nameof(id));
        }

        var now = time.GetUtcNow();
        var user = new User(id, email, role, profile, registration, now);
        user.Record(now, null, HistoryAction.Registered, details: registration.Source.ToString());
        user._domainEvents.Add(new UserRegistered(id, profile.DisplayName.Value, profile.AvatarUrl?.Value));
        return user;
    }

    private void Record(DateTimeOffset now, Guid? actorId, HistoryAction action, string? reason = null, string? details = null)
    {
        _history.Add(HistoryEntry.Create(Id, now, actorId, action, reason, details));
        UpdatedAt = now;
    }
}
