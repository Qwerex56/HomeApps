namespace Accounts.Domain.Users;

/// <summary>
/// One line of the account's append-only history (accounts.md 3.2): who did what and when. Stores which profile fields
/// changed, never their values. <see cref="ActorId"/> is null for changes coming from Auth.
/// </summary>
public sealed class HistoryEntry
{
    private HistoryEntry() { }

    private HistoryEntry(Guid id, Guid userId, DateTimeOffset at, Guid? actorId, HistoryAction action, string? reason, string? details)
    {
        Id = id;
        UserId = userId;
        At = at;
        ActorId = actorId;
        Action = action;
        Reason = reason;
        Details = details;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset At { get; private set; }

    public Guid? ActorId { get; private set; }

    public HistoryAction Action { get; private set; }

    public string? Reason { get; private set; }

    /// <summary>E.g. the changed field names (<c>DisplayName, Locale</c>) or the new role.</summary>
    public string? Details { get; private set; }

    internal static HistoryEntry Create(
        Guid userId, DateTimeOffset at, Guid? actorId, HistoryAction action, string? reason = null, string? details = null) =>
        new(Guid.CreateVersion7(at), userId, at, actorId, action, reason, details);
}
